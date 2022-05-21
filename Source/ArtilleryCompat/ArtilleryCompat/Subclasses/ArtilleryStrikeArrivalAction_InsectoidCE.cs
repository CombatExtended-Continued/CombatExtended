using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;
using HarmonyLib;
using VFESecurity;

namespace CombatExtended.Compatibility.Artillery
{

    public class ArtilleryStrikeArrivalAction_InsectoidCE : ArtilleryStrikeArrivalAction_Insectoid
    {
	public ArtilleryStrikeArrivalAction_InsectoidCE() {}

	public ArtilleryStrikeArrivalAction_InsectoidCE(WorldObject worldObject, Map sourceMap)
        {
            this.worldObject = worldObject;
            this.sourceMap = sourceMap;
        }

	public override void Arrived(List<ActiveArtilleryStrike> artilleryStrikes, int tile)
	{
	    this.ArrivedCE(artilleryStrikes, tile);
	}
	public override void StrikeAction(ActiveArtilleryStrike strike, CellRect mapRect, CellRect baseRect, ref bool destroyed)
        {
            var radialCells = GenRadial.RadialCellsAround(mapRect.RandomCell, strike.shellDef.GetProjectileProperties().explosionRadius, true);
            int cellsInRect = radialCells.Count(c => baseRect.Contains(c));

            // Aggro the insects
            if (cellsInRect > 0 && Rand.Chance(cellsInRect * DestroyChancePerCellInRect))
            {
                var artilleryComp = Settlement.GetComponent<ArtilleryComp>();
                var parms = new IncidentParms();
                parms.target = sourceMap;
                parms.points = StorytellerUtility.DefaultThreatPointsNow(sourceMap) * (1 + ((float)artilleryComp.recentRetaliationTicks / RetaliationTicksPerExtraPointsMultiplier));
                parms.faction = Settlement.Faction;
                parms.generateFightersOnly = true;
                parms.forced = true;
                Find.Storyteller.incidentQueue.Add(IncidentDefOf.RaidEnemy, Find.TickManager.TicksGame + RaidIntervalRange.RandomInRange, parms);
                artilleryComp.recentRetaliationTicks += RetaliationTicksPerRetaliation;
            }
        }
    }
}
