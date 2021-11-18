using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CombatExtended
{
    public class WorldStrengthTracker : WorldComponent
    {
        private bool initialized = false;
        private List<FactionStrengthTracker> trackers = new List<FactionStrengthTracker>();

        public WorldStrengthTracker(World world) : base(world) 
        {
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (!initialized)
            {
                Rebuild();
            }
            if(GenTicks.TicksGame % 30000 == 0)
            {
                Rebuild();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            try
            {
                Scribe_Collections.Look(ref trackers, "trackers", LookMode.Deep);
            }
            catch(Exception er)
            {
                Log.Error($"CE: WorldStrengthTracker failed to scribe, rebuilding.");
                Log.Error($"CE: {er}");
                trackers.Clear();
            }
            trackers ??= new List<FactionStrengthTracker>();
        }

        public FactionStrengthTracker GetFactionTracker(Faction faction)
        {
            if(faction == null)
            {
                return null;
            }
            if(faction.defeated || faction.IsPlayer)
            {
                return null;
            }
            FactionStrengthTracker tracker = trackers.FirstOrDefault(t => t.Faction == faction);
            if (tracker != null)
            {
                return tracker;
            }
            Rebuild();
            return trackers.FirstOrDefault(t => t.Faction == faction);
        }

        public void Rebuild()
        {
            trackers.RemoveAll(t => t.Faction.defeated);
            foreach (Faction faction in world.factionManager.AllFactions)
            {
                if (faction.IsPlayer)
                {
                    continue;
                }
                if (faction.defeated)
                {
                    continue;
                }
                if(trackers.Any(s => s.Faction == faction))
                {
                    continue;
                }
                FactionStrengthTracker tracker = new FactionStrengthTracker(faction);
                trackers.Add(tracker);
            }            
        }
    }
}

