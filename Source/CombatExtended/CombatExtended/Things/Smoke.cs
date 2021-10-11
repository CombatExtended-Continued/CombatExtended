using System.Collections.Generic;
using System.Linq;
using CombatExtended.AI;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class Smoke : Gas
    {
        public const int UpdateIntervalTicks = 30;
        private const float InhalationPerSec = 0.045f * UpdateIntervalTicks / GenTicks.TicksPerRealSecond;
        private const float DensityDissipationThreshold = 3.0f;
        private const float MinSpreadDensity = 1.0f;    //ensures smoke clouds don't spread to infinitely small densities. Should be lower than DensityDissipationThreshold to avoid clouds stuck indoors.
        private const float MaxDensity = 12800f;
        private const float BugConfusePercent = 0.15f;  // Percent of MaxDensity where bugs go into confused wander
        private const float LethalAirPPM = 1000f;       // Level of PPM where target severity hits 100% (about 2x the WHO/FDA immediately-dangerous-to-everyone threshold).

        private float density;
        private int updateTickOffset;   //Random offset (it looks jarring when smoke clouds all update on the same tick)

        private DangerTracker _dangerTracker = null;
        private DangerTracker DangerTracker
        {
            get
            {
                return _dangerTracker ?? (_dangerTracker = Map.GetDangerTracker());
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            updateTickOffset = Rand.Range(0, UpdateIntervalTicks);
            base.SpawnSetup(map, respawningAfterLoad);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref density, "density");
        }

        public override string LabelNoCount
        {
            get
            {
                return base.LabelNoCount + " (" + Mathf.RoundToInt(density) + " ppm)";
            }
        }

        private bool CanMoveTo(IntVec3 pos)
        {
            return
                pos.InBounds(Map)
                && (
                    !pos.Filled(Map)
                    || (pos.GetDoor(Map)?.Open ?? false)
                    || (pos.GetFirstThing<Building_Vent>(Map) is Building_Vent vent && vent.TryGetComp<CompFlickable>().SwitchIsOn)
                );
        }

        public override void Tick()
        {
	    if (density > DensityDissipationThreshold)   //very low density smoke clouds eventually dissipate on their own
            {
                destroyTick++;
		if (Rand.Range(0,10) == 5) {
		  float d = density * 0.0001f;
		  density -= d;
		  if (Random.Range(0, (int)(0.01*MaxDensity)) < d) {
		    FilthMaker.TryMakeFilth(Position, Map, ThingDefOf.Filth_Ash, 1, FilthSourceFlags.None);
		  }
		}
            }

            if ((GenTicks.TicksGame + updateTickOffset) % UpdateIntervalTicks == 0)
            {
                if (!CanMoveTo(Position))   //cloud is in inaccessible cell, probably a recently closed door or vent. Spread to nearby cells and delete.
                {
                    SpreadToAdjacentCells();
                    Destroy();
                    return;
                }
                if (!Position.Roofed(Map))
                {
                    UpdateDensityBy(-60);
                }
                SpreadToAdjacentCells();
                ApplyHediffs();
            }
            if (this.IsHashIntervalTick(120))
                DangerTracker?.Notify_SmokeAt(Position);

            base.Tick();
        }

        private void ApplyHediffs()
        {
            if (!Position.InBounds(Map))
                return;

            var pawns = Position.GetThingList(Map).Where(t => t is Pawn).ToList();
	    var baseTargetSeverity = Mathf.Pow(density / LethalAirPPM, 2);
	    var baseSeverityRate = InhalationPerSec * Mathf.Pow(density / MaxDensity, 2);

            foreach (Pawn pawn in pawns)
            {
                if (pawn.RaceProps.FleshType == FleshTypeDefOf.Insectoid)
                {
                    if (density > MaxDensity * BugConfusePercent)
                        pawn.mindState.mentalStateHandler.TryStartMentalState(CE_MentalStateDefOf.WanderConfused);
                    continue;
                }
                pawn.TryGetComp<CompTacticalManager>()?.GetTacticalComp<CompGasMask>()?.Notify_ShouldEquipGasMask();
		var sensitivity = pawn.GetStatValue(CE_StatDefOf.SmokeSensitivity);
                var severity = baseSeverityRate * sensitivity;

		if (baseTargetSeverity * sensitivity > severity) {
		  HealthUtility.AdjustSeverity(pawn, CE_HediffDefOf.SmokeInhalation, severity);
		}
            }
        }

        private void SpreadToAdjacentCells()
        {
            if (density >= MinSpreadDensity)
            {
                var freeCells = GenAdjFast.AdjacentCellsCardinal(Position).InRandomOrder().Where(CanMoveTo).ToList();
                foreach (var freeCell in freeCells)
                {
                    if (freeCell.GetGas(Map) is Smoke existingSmoke)
                    {
                        var densityDiff = this.density - existingSmoke.density;
                        TransferDensityTo(existingSmoke, densityDiff / 2);
                    }
                    else
                    {
                        var newSmokeCloud = (Smoke)GenSpawn.Spawn(CE_ThingDefOf.Gas_BlackSmoke, freeCell, Map);
                        TransferDensityTo(newSmokeCloud, this.density / 2);
                    }
                }
            }
        }

        public void UpdateDensityBy(float diff)
        {
            density = Mathf.Clamp(density + diff, 0, MaxDensity);
        }

        private void TransferDensityTo(Smoke target, float value)
        {
            this.UpdateDensityBy(-value);
            target.UpdateDensityBy(value);
        }

        public float GetOpacity()
        {
            return 0.05f + (0.95f * (density / MaxDensity));
        }
    }
}
