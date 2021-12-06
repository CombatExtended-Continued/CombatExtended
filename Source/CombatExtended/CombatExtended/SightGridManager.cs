using System;
using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using System.Threading.Tasks;
using System.Threading;

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
        public readonly int bucketCount;
        public readonly int updateInterval;        

        private List<IThingSightRecord> tmpRecords = new List<IThingSightRecord>();       

        private int phase;
        private int ticksUntilUpdate;        
        private int curIndex;
        private ThreadStart threadStart;
        private Thread thread;
        private object locker = new object();
        private readonly Dictionary<T, IThingSightRecord> records = new Dictionary<T, IThingSightRecord>();
        private readonly List<IThingSightRecord>[] pool;
        private readonly List<Action> castingQueue = new List<Action>();

        protected readonly float PerformanceFactorMaxThings = 50;
        protected readonly float GroupingRange = 4;
        protected readonly float GroupingMaxRangeDelta = 10;
        protected readonly float MinRange = 3;      

        private bool mapIsAlive = true;
        private bool wait = false;

        public SightGridManager(SightTracker tracker, int bucketCount, int updateInterval)
        {
            map = tracker.map;
            this.tracker = tracker;
            grid = new SightGrid(map);

            ticksUntilUpdate = updateInterval;
            this.updateInterval = updateInterval;
            this.bucketCount = bucketCount;
            phase = Rand.Range(1, 17);

            pool = new List<IThingSightRecord>[this.bucketCount];
            for (int i = 0; i < this.bucketCount; i++)
                pool[i] = new List<IThingSightRecord>();

            threadStart = new ThreadStart(OffMainThreadLoop);
            thread = new Thread(threadStart);
            thread.Start();
        }

        public virtual void Tick()
        {
            if (ticksUntilUpdate-- > 0 || wait)
                return;
            
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
                TryCastSight(record);                                      
            }
            if(tmpRecords.Count != 0)
            {
                for (int i = 0; i < tmpRecords.Count; i++)                                            
                    DeRegister(tmpRecords[i].thing);
                // clean up.
                tmpRecords.Clear();
            }                        
            curIndex++;
            ticksUntilUpdate = updateInterval;
            if (curIndex >= bucketCount)
            {
                wait = true;
                lock (locker)
                {
                    castingQueue.Add(delegate
                    {
                        grid.NextCycle();
                        OnFinishedCycle();
                        wait = false;
                    });
                }                
                curIndex = 0;                                                 
            }                                                 
        }

        public virtual void Register(T thing)
        {
            if(Valid(thing) && !records.TryGetValue(thing, out IThingSightRecord record))
            {
                record = new IThingSightRecord();
                record.thing = thing;
                record.bucketIndex = (thing.thingIDNumber + 19) % bucketCount;                
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
        public virtual void Notify_MapRemoved()
        {
            try
            {
                mapIsAlive = false;
                thread.Join();
            }
            catch(Exception er)
            {
                Log.Error($"CE: SightGridManager Notify_MapRemoved failed to stop thread with {er}");
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
                foreach(T thing in ThingsInRange(record.thing.Position, 2 * GroupingRange * 0.5f))
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
            lock (locker)
            {                
                castingQueue.Add(delegate
                {
                    grid.Next(pos, range, GetFlags(record));
                    grid.Set(pos, count, 1);
                    ShadowCastingUtility.CastVisibility(map, pos, (cell, dist) => grid.Set(cell, count, dist), range);
                });                
            }           
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

            float distanceTraveled = Mathf.Min(pawn.GetStatValue(StatDefOf.MoveSpeed) * (updateInterval * bucketCount) / 60f, path.NodesLeftCount - 1);            
            return path.Peek(Mathf.FloorToInt(distanceTraveled));            
        }

        private void OffMainThreadLoop()
        {
            Action castAction;
            int castActionsLeft;
            while (mapIsAlive)
            {
                castAction = null;
                castActionsLeft = 0;
                lock (locker)
                {
                    if ((castActionsLeft = castingQueue.Count) > 0)
                    {
                        castAction = castingQueue[0];
                        castingQueue.RemoveAt(0);
                    }
                }
                // threading goes brrrrrr
                if (castAction != null)
                    castAction.Invoke();
                // sleep so other threads can do stuff
                if(castActionsLeft == 0)
                    Thread.Sleep(1);
            }
            Log.Message("CE: SightGridManager thread stopped!");
        }        
    }
}

