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
    // TODO Rework all of this
    //
    //
    public class HostilityRaider : IExposable
    {
        private int ticksToRaid = -1;

        private IncidentParms parms;

        private float points = -1;

        public HostilityComp comp;
        public virtual bool AbleToRaidResponse
        {
            get
            {
                FactionStrengthTracker tracker = comp.parent.Faction.GetStrengthTracker();
                if (tracker == null || !tracker.CanRaid)
                {
                    return false;
                }
                bool? res = null;
                if (comp.parent is Site site)
                {
                    foreach (var sitePart in site.parts)
                    {
                        res = sitePart.def.GetModExtension<WorldObjectHostilityExtension>()?.AbleToRaidResponse;
                        if (res.HasValue)
                        {
                            return res.Value;
                        }
                    }
                }
                res = comp.parent.Faction?.def.GetModExtension<WorldObjectHostilityExtension>()?.AbleToRaidResponse;
                if (res.HasValue)
                {
                    return res.Value;
                }
                res = (comp.props as WorldObjectCompProperties_Hostility).AbleToRaidResponse;
                if (res.HasValue)
                {
                    return res.Value;
                }
                return tracker?.CanRaid ?? false;
            }
        }

        public HostilityRaider()
        {
        }

        public virtual void ThrottledTick()
        {
            if (ticksToRaid > 0 || points < 0)
            {
                ticksToRaid -= WorldObjectTrackerCE.THROTTLED_TICK_INTERVAL;
                return;
            }
            if (parms != null && Find.Maps.Contains(parms.target))
            {
                IncidentDef incidentDef = IncidentDefOf.RaidEnemy;
                incidentDef.Worker.TryExecute(parms);
            }
            points = -1;
            parms = null;
            ticksToRaid = -1;
        }

        // TODO
        // Move this to a queue based system
        public virtual bool TryRaid(Map targetMap, float points)
        {
            if (!AbleToRaidResponse)
            {
                return false;
            }

            if (points <= 0)
            {
                return false;
            }
            this.points = points;
            ticksToRaid = Rand.Range(3000, 15000);

            string factionName = comp.parent.Faction.Name;
            string objectName = comp.parent.Label;

            StorytellerComp storytellerComp = Find.Storyteller.storytellerComps.First((StorytellerComp x) => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
            parms = storytellerComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, Find.CurrentMap);
            parms.faction = comp.parent.Faction;
            parms.points = points;
            parms.customLetterDef = CE_LetterDefOf.CE_ThreatBig;
            parms.customLetterLabel = "CE_Message_CounterRaid_Label".Translate(factionName);
            parms.customLetterText = "CE_Message_CounterRaid_Desc".Translate(factionName, objectName);
            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            if (comp.parent.Faction.def.techLevel >= TechLevel.Industrial)
            {

                if (Rand.Chance(Mathf.Min(points / 10000, 0.5f)))
                {
                    parms.raidArrivalMode = PawnsArrivalModeDefOf.CenterDrop;
                }
                else if (Rand.Chance(0.25f))
                {
                    parms.raidArrivalMode = PawnsArrivalModeDefOf.EmergeFromWater;
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
            return true;
        }

        public void ExposeData()
        {
            Scribe_Deep.Look(ref parms, "raidParms");
            Scribe_Values.Look(ref points, "points");
            Scribe_Values.Look(ref ticksToRaid, "ticksToRaid");
        }
    }
}

