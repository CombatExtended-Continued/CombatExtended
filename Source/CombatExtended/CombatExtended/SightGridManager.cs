using System;
using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public abstract class SightGridManager<T> where T : Thing
    {        
        protected class IThingSightRecord
        {
            /// <summary>
            /// The bucket index of the owner pawn.
            /// </summary>
            public int bucketIndex;
            /// <summary>
            /// Owner pawn.
            /// </summary>
            public T thing;
            /// <summary>
            /// The tick at which this pawn was updated.
            /// </summary>
            public int lastCycle;
            /// <summary>
            /// Last range of the owner thing
            /// </summary>
            public int lastRange;
            /// <summary>
            /// Number of other pawns skipping casting by using this pawn.
            /// </summary>
            public int carry;
            /// <summary>
            /// The sum of ranges of the carried pawns.
            /// </summary>
            public int carryRange;
            /// <summary>
            /// The sum of positions for the carried pawns.
            /// </summary>
            public IntVec3 carryPosition;          
        }

        public readonly Map map;
        public readonly SightTracker tracker;
        public readonly SightGrid grid;
        public readonly int BucketCount;
        public readonly int MinUpdateInterval;
        public readonly int MaxUpdateInterval;

        private List<IThingSightRecord> tmpRecords = new List<IThingSightRecord>();
        
        private int phase;
        private int ticksUntilUpdate;
        private int curUpdateInterval;
        private int curIndex;
        private int curNumThingCasted;
        private float performanceFactor = 0.5f;
        private readonly Stopwatch stopwatch = new Stopwatch();        
        private readonly Dictionary<T, IThingSightRecord> records = new Dictionary<T, IThingSightRecord>();
        private readonly List<IThingSightRecord>[] pool;

        protected readonly float PerformanceFactorMaxThings = 50;
        protected readonly float GroupingRange = 4;
        protected readonly float GroupingMaxRangeDelta = 10;
        protected readonly float MinRange = 3;

        protected virtual float PerformanceFactor
        {
            get => performanceFactor;
        }

        public SightGridManager(SightTracker tracker, int bucketCount = 20, int minUpdateInterval = 10, int maxUpdateInterval = 30)
        {
            map = tracker.map;
            this.tracker = tracker;
            grid = new SightGrid(map);            
            phase = Rand.Range(minUpdateInterval, maxUpdateInterval);
            MinUpdateInterval = minUpdateInterval;
            MaxUpdateInterval = maxUpdateInterval;
            curUpdateInterval = (MaxUpdateInterval + MinUpdateInterval) / 2;
            ticksUntilUpdate = curUpdateInterval;
            BucketCount = bucketCount;
            pool = new List<IThingSightRecord>[BucketCount];

            for (int i = 0; i < BucketCount; i++)
                pool[i] = new List<IThingSightRecord>();            
        }        

        public virtual void Tick()
        {
            if(ticksUntilUpdate-- < 0)
            {                
                stopwatch.Reset();
                stopwatch.Start();
                tmpRecords.Clear();
                List<IThingSightRecord> curPool = pool[curIndex];

                for(int i = 0; i < curPool.Count; i++)
                {
                    IThingSightRecord record = curPool[i];
                    if(!Valid(record.thing))
                    {
                        tmpRecords.Add(record);
                        continue;
                    }
                    // cast sight.
                    if (TryCastSight(record))                    
                        curNumThingCasted++;                    
                }
                if(tmpRecords.Count != 0)
                {
                    for (int i = 0; i < tmpRecords.Count; i++)                                            
                        DeRegister(tmpRecords[i].thing);
                    // clean up.
                    tmpRecords.Clear();
                }
                OnBucketCasted(curPool);
                curIndex++;
                stopwatch.Stop();
                if (curIndex >= BucketCount)
                {
                    performanceFactor = 0.5f - Mathf.Min(curNumThingCasted, PerformanceFactorMaxThings) / PerformanceFactorMaxThings * 0.35f;
                    curIndex = 0;
                    curNumThingCasted = 0;                    
                    grid.NextCycle();
                    OnFinishedCycle();
                }                
                //float tms = (float)stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000;
                //curUpdateInterval = (int) Mathf.Lerp(MinUpdateInterval, MaxUpdateInterval, tms / 8);
                ticksUntilUpdate = curUpdateInterval;
            }
        }

        public virtual void Register(T thing)
        {
            if(Valid(thing) && !records.TryGetValue(thing, out IThingSightRecord record))
            {
                record = new IThingSightRecord();
                record.thing = thing;
                record.bucketIndex = (thing.thingIDNumber + 19) % BucketCount;                
                records.Add(thing, record);
                pool[record.bucketIndex].Add(record);
            }
        }
        public virtual void DeRegister(T thing)
        {            
            if (thing != null && records.TryGetValue(thing, out IThingSightRecord record))
            {
                pool[record.bucketIndex].Remove(record);
                records.Remove(record.thing);
            }
        }
        
        protected abstract bool Skip(IThingSightRecord record);        
        protected abstract int GetSightRange(IThingSightRecord record);        
        protected abstract IEnumerable<T> ThingsInRange(IntVec3 position, float range);

        protected virtual UInt64 GetFlags(IThingSightRecord record) => 0;
        protected virtual bool CanGroup(IThingSightRecord first, IThingSightRecord second) => true;
        protected virtual bool Valid(T thing)
        {
            if (thing == null)
                return false;
            if (!thing.Spawned)
                return false;
            return true;
        }
        protected virtual void OnFinishedCycle()
        {
        }
        protected virtual void OnBucketCasted(List<IThingSightRecord> bucket)
        {
        }

        private bool TryCastSight(IThingSightRecord record)
        {
            if (grid.CycleNum == record.lastCycle || Skip(record))
                return false;            

            int range = GetSightRange(record);
            if (range < MinRange)
                return false;

            if (record.carry == 0)
            {
                IThingSightRecord best = null;
                int bestExtras = -1;
                foreach(T thing in ThingsInRange(record.thing.Position, 2 * GroupingRange * (1 - performanceFactor)))
                {
                    if (thing != record.thing && records.TryGetValue(thing, out IThingSightRecord s))
                    {
                        if(grid.CycleNum - s.lastCycle == 1
                            && s.lastRange - range > -GroupingMaxRangeDelta
                            && s.carry > bestExtras
                            && CanGroup(record, s))
                        {
                            best = s;
                            bestExtras = s.carry;
                        }
                    }
                }
                if (best != null)
                {
                    best.carryRange += (int)range;
                    best.carryPosition += GetShiftedPosition(record);
                    best.carry++;
                    record.lastCycle = grid.CycleNum;
                    return false;
                }
            }
            IntVec3 pos = GetShiftedPosition(record);
            if (!pos.InBounds(map))
            {
                Log.Error($"CE: SighGridUpdater {record.thing} position is outside the map's bounds!");
                return false;
            }
            int count = record.carry + 1;
            grid.Next(pos, range, GetFlags(record));            
            grid.Set(pos, count, 1);
            ShadowCastingUtility.CastVisibility(map, pos, (cell, dist) => grid.Set(cell, count, dist), range);

            record.carry = 0;
            record.carryRange = 0;
            record.carryPosition = IntVec3.Zero;
            record.lastCycle = grid.CycleNum;
            record.lastRange = range;
            return true;
        }

        private IntVec3 GetShiftedPosition(IThingSightRecord record)
        {
            IntVec3 position = record.carryPosition;
            if (record.thing is Pawn pawn)
                position += GetMovingShiftedPosition(pawn);
            else
                position += record.thing.Position;
            position.x /= (1 + record.carry);
            position.z /= (1 + record.carry);
            return position;
        }

        private IntVec3 GetMovingShiftedPosition(Pawn pawn)
        {
            PawnPath path;

            if (!(pawn.pather?.moving ?? false) || (path = pawn.pather.curPath) == null || path.NodesLeftCount <= 1)
                return pawn.Position;

            float distanceTraveled = Mathf.Min(pawn.GetStatValue(StatDefOf.MoveSpeed) * (curUpdateInterval * BucketCount) / 60f, path.NodesLeftCount - 1);            
            return path.Peek(Mathf.FloorToInt(distanceTraveled));            
        }        
    }
}

