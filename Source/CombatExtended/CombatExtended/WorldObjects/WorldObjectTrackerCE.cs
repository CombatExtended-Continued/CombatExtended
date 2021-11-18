using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CombatExtended.WorldObjects
{
    /// <summary>
    /// Used to allow the spacing of ticking of world object components without affecting performance by spamming calls.
    /// </summary>
    public class WorldObjectTrackerCE : WorldComponent
    {
        public const int THROTTLED_TICK_INTERVAL = 30;

        private class TrackedObject
        {            
            public readonly WorldObject worldObject;
            public readonly List<IWorldCompCE> compsCE;

            public bool IsValid => !compsCE.NullOrEmpty() && worldObject != null && !worldObject.destroyed;             

            public TrackedObject(RimWorld.Planet.WorldObject worldObject, List<IWorldCompCE> compsCE)
            {
                this.worldObject = worldObject;
                this.compsCE = compsCE;
            }

            public void ThrottledCompsTick()
            {
                if (!IsValid)
                {
                    return;
                }
                for(int i = 0; i < compsCE.Count; i++)
                {
                    compsCE[i].ThrottledCompTick();
                }
            }
        }

        private int cleanUpIndex = 0;
        private int updateIndex = 0;        

        private List<TrackedObject>[] trackedObjects = new List<TrackedObject>[THROTTLED_TICK_INTERVAL];

        public IEnumerable<WorldObject> TrackedObjects
        {
            get
            {
                for (int i = 0; i < THROTTLED_TICK_INTERVAL; i++)
                {
                    List<TrackedObject> items = trackedObjects[i];
                    for(int j = 0;j < items.Count; j++)
                    {
                        if (!items[j].IsValid)
                        {
                            continue;
                        }
                        yield return items[j].worldObject;                        
                    }
                }
            }
        }

        public WorldObjectTrackerCE(World world) : base(world)
        {
            for(int i = 0; i < THROTTLED_TICK_INTERVAL; i++)
            {
                trackedObjects[i] = new List<TrackedObject>();
            }
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            int ticks = GenTicks.TicksGame;
            if (ticks % GenTicks.TickRareInterval == 0)
            {
                trackedObjects[cleanUpIndex].RemoveAll(u => !u.IsValid);
                cleanUpIndex = (cleanUpIndex + 1) % THROTTLED_TICK_INTERVAL;
            }
            if (!trackedObjects[updateIndex].NullOrEmpty())
            {
                List<TrackedObject> items = trackedObjects[updateIndex];
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].IsValid)
                    {
                        try
                        {
                            items[i].ThrottledCompsTick();
                        }
                        catch (Exception er)
                        {
                            Log.Error($"CE: Error while updating WorldUpdatable {items[i]} {er}");
                        }
                    }
                }
            }
            updateIndex = (updateIndex + 1) % THROTTLED_TICK_INTERVAL;
        }

        public void TryRegister(WorldObject worldObject)
        {
            List<IWorldCompCE> compsCE = worldObject.GetCompsCE().ToList();
            if (compsCE.NullOrEmpty())
            {
                return;
            }            
            int minCountIndex = 0;
            int minCount = trackedObjects[0].Count;
            for (int i = 0; i < THROTTLED_TICK_INTERVAL; i++)
            {
                if (trackedObjects[i].Any(u => u.worldObject == worldObject))
                {
                    return;
                }
                if (trackedObjects[i].Count < minCount || (trackedObjects[i].Count == minCount && Rand.Chance(0.5f)))
                {
                    minCountIndex = i;
                    minCount = trackedObjects[i].Count;
                }
            }
            TrackedObject element = new TrackedObject(worldObject, compsCE);            
            trackedObjects[minCountIndex].Add(element);
        }

        public void TryDeRegister(WorldObject worldObject)
        {
            for (int i = 0; i < THROTTLED_TICK_INTERVAL; i++)
            {
                trackedObjects[i].RemoveAll(u => !u.IsValid || u.worldObject == worldObject);
            }
        }        
    }
}

