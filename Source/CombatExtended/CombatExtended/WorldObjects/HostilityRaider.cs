using System;
using System.Collections.Generic;
using System.Linq;
using CombatExtended.WorldObjects;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
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
            if (targetInfo.IsValid)
            {
                DoRaid();
            }
            points = -1;
            targetInfo = GlobalTargetInfo.Invalid;
            ticksToRaid = -1;
        }

        public bool TryRaid(Map targetMap, float points)
        {
            FactionStrengthTracker tracker =  comp.parent.Faction.GetStrengthTracker();
            if (tracker != null && !tracker.CanRaid)
            {
                return false;
            }
            if (points <= 0)
            {
                return false;
            }
            this.points = points;
            targetInfo = new GlobalTargetInfo(IntVec3.Zero, targetMap);
            targetInfo.tileInt = targetMap.Tile;                        
            ticksToRaid = Rand.Range(3000, 15000);            
            return true;
        }

        private void DoRaid()
        {
            string factionName = $"<color=red>{comp.parent.Faction.Name}</color>";
            string objectName = $"<color=blue>{comp.parent.Label}</color>";
            StorytellerComp storytellerComp = Find.Storyteller.storytellerComps.First((StorytellerComp x) => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
            IncidentParms parms = storytellerComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, Find.CurrentMap);
            parms.faction = comp.parent.Faction;
            parms.points = points;
            parms.customLetterDef = CE_LetterDefOf.CE_ThreatBig;            
            parms.customLetterLabel = "CE_Message_CounterRaid_Label".Translate(factionName);
            parms.customLetterText = "CE_Message_CounterRaid_Desc".Translate(factionName, objectName);
            if (comp.parent.Faction.def.techLevel >= TechLevel.Industrial)
            {
                if(Rand.Chance(Mathf.Min(points / 5000, 0.5f)))
                {
                    parms.raidArrivalMode =  PawnsArrivalModeDefOf.CenterDrop;
                }
                else
                {
                    parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeDrop;
                }
            }
            else
            {
                parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
            }            
            IncidentDef incidentDef = IncidentDefOf.RaidEnemy;            
            List<RaidStrategyDef> source = DefDatabase<RaidStrategyDef>.AllDefs.Where((RaidStrategyDef s) => s.Worker.CanUseWith(parms, PawnGroupKindDefOf.Combat)).ToList();                            
            parms.raidStrategy = source.RandomElement();
            incidentDef.Worker.TryExecute(parms);
        }

        public void ExposeData()
        {            
            Scribe_TargetInfo.Look(ref targetInfo, "targetInfo");
            Scribe_Values.Look(ref points, "points");
            Scribe_Values.Look(ref ticksToRaid, "ticksToRaid");
        }
    }
}

