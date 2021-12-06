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
    public abstract class SightTracker<T> where T : Thing
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
        }

        public readonly Map map;
        public readonly SightTracker parent;
        public readonly SightGrid grid;
        public readonly int bucketCount;
        public readonly int updateInterval;        

        private List<IThingSightRecord> tmpRecords = new List<IThingSightRecord>();       
        
        private int ticksUntilUpdate;        
        private int curIndex;
        private ThreadStart threadStart;
        private Thread thread;
        private object locker = new object();
        private readonly Dictionary<T, IThingSightRecord> records = new Dictionary<T, IThingSightRecord>();
        private readonly List<IThingSightRecord>[] pool;
        private readonly List<Action> castingQueue = new List<Action>();
        
        private bool mapIsAlive = true;
        private bool wait = false;

        public SightTracker(SightTracker tracker, int bucketCount, int updateInterval)
        {            
            this.parent = tracker;
            this.updateInterval = updateInterval;
            this.bucketCount = bucketCount;            
            grid = new SightGrid(map = tracker.map);
            
            ticksUntilUpdate = updateInterval;
            
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
                        lock (locker)
                        {
                            grid.NextCycle();
                            OnFinishedCycle();
                            wait = false;
                        }
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

        protected virtual UInt64 GetFlags(IThingSightRecord record) => 0;        
        protected virtual bool Valid(T thing)
        {
            if (thing == null)
                return false;
            if (thing.Faction == null)
                return false;
            if (!thing.Spawned)
                return false;
            return true;
        }
        protected virtual void OnFinishedCycle()
        {
        }        

        private bool TryCastSight(IThingSightRecord record)
        {
            if (grid.CycleNum == record.lastCycle || Skip(record))
                return false;            

            int range = GetSightRange(record);
            if (range < 3)
                return false;

            IntVec3 pos = GetShiftedPosition(record);
            if (!pos.InBounds(map))
            {
                Log.Error($"CE: SighGridUpdater {record.thing} position is outside the map's bounds!");
                return false;
            }
            int count = 1;
            lock (locker)
            {                
                castingQueue.Add(delegate
                {
                    grid.Next(pos, range, GetFlags(record));
                    grid.Set(pos, count, 1);
                    ShadowCastingUtility.CastVisibility(map, pos, (cell, dist) => grid.Set(cell, count, dist), range);
                });                
            }                       
            record.lastCycle = grid.CycleNum;            
            return true;
        }        

        private IntVec3 GetShiftedPosition(IThingSightRecord record)
        {            
            if (record.thing is Pawn pawn)
                return GetMovingShiftedPosition(pawn);
            else
                return record.thing.Position;            
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

