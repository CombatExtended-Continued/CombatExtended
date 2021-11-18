using System;
using System.Collections.Generic;
using System.Linq;
using CombatExtended.WorldObjects;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CombatExtended.WorldObjects
{
    public class HostilityRaider : IExposable
    {
        public HostilityComp comp;

        private int ticksToRaid = -1;
        private GlobalTargetInfo targetInfo;        
        private float points = -1;

        public HostilityRaider()
        {
        }

        public void ThrottledTick()
        {
            if (ticksToRaid > 0 || points < 0)
            {
                ticksToRaid -= WorldObjectTrackerCE.THROTTLED_TICK_INTERVAL;
                return;
            }
            //if (targetInfo.IsValid)
            //{
            //    DoRaid();
            //}
            //points = -1;
            //targetInfo = GlobalTargetInfo.Invalid;
        }

        public bool TryRaid(Map targetMap, float points)
        {
            if (!comp.parent.Faction.GetStrengthTracker().CanRaid && Rand.Chance(0.5f))
            {
                return false;
            }
            //if (points <= 0)
            //{
            //    return false;
            //}            
            //this.points = points;
            //targetInfo = new GlobalTargetInfo();
            //targetInfo.tileInt = targetMap.Tile;
            //targetInfo.mapInt = targetMap;
            //targetInfo.cellInt = new IntVec3(1, 1, 1); // for scribing
            //ticksToRaid = Rand.Range(3000, 30000);            
            return true;
        }

        private void DoRaid()
        {            
            StorytellerComp storytellerComp = Find.Storyteller.storytellerComps.First((StorytellerComp x) => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
            IncidentDef incidentDef = IncidentDefOf.RaidEnemy;
            IncidentParms parms = storytellerComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, targetInfo.Map);
            List<RaidStrategyDef> source = DefDatabase<RaidStrategyDef>.AllDefs.Where((RaidStrategyDef s) => s.Worker.CanUseWith(parms, PawnGroupKindDefOf.Combat)).ToList();

            parms.faction = comp.parent.Faction;
            parms.points = points;
            parms.raidStrategy = source.RandomElement();
            incidentDef.Worker.TryExecute(parms);
        }

        public void ExposeData()
        {            
            Scribe_TargetInfo.Look(ref targetInfo, "raid_targetInfo");
            Scribe_Values.Look(ref points, "raid_points");
            Scribe_Values.Look(ref ticksToRaid, "ticksToRaid");
        }
    }
}

