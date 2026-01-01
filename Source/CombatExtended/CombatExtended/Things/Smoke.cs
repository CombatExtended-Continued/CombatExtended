using System.Collections.Generic;
using CombatExtended.AI;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended;
public class Smoke : Gas
{
    public const int UpdateIntervalTicks = 60;
    private const float InhalationPerSec = 0.045f * UpdateIntervalTicks / GenTicks.TicksPerRealSecond;
    private const float DensityDissipationThreshold = 30.0f;
    private const float MinSpreadDensity = 10.0f;    //ensures smoke clouds don't spread to infinitely small densities. Should be lower than DensityDissipationThreshold to avoid clouds stuck indoors.
    private const float MaxDensity = 12800f;
    private const float BugConfusePercent = 0.15f;  // Percent of MaxDensity where bugs go into confused wander
    private const float LethalAirPPM = 10000f;       // Level of PPM where target severity hits 100% (about 2x the WHO/FDA immediately-dangerous-to-everyone threshold).

    private struct DensityTransfer
    {
        public Smoke Target;
        public IntVec3 Position;
        public float Amount;
    }

    /// <summary>
    /// List of pending density transfers to neighboring cells.
    /// </summary>
    private List<DensityTransfer> _transfers = [];

    private float density;

    private DangerTracker _dangerTracker = null;
    private DangerTracker DangerTracker
    {
        get
        {
            return _dangerTracker ?? (_dangerTracker = Map.GetDangerTracker());
        }
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
            return base.LabelNoCount + " (" + Mathf.RoundToInt(density / 10f) + " ppm)";
        }
    }

    /// <summary>
    /// Overridden to a fixed tick rate, this also avoids expensive checks for whether the smoke is in the viewport.
    /// </summary>
    public override int UpdateRateTicks => 15;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        // BlackSmokeTracker manages ticking for Smoke instances.
        Map.GetComponent<BlackSmokeTracker>().Register(this);
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        Map.GetComponent<BlackSmokeTracker>().Unregister(this);
        base.DeSpawn(mode);
    }

    private bool CanMoveTo(IntVec3 pos)
    {
        Map map = Map;
        if (!pos.InBounds(map))
        {
            return false;
        }

        Building edifice = pos.GetEdifice(map);
        if (edifice?.def.Fillage != FillCategory.Full)
        {
            return true;
        }

        return edifice is Building_Door { Open: true } ||
               edifice is Building_Vent vent && FlickUtility.WantsToBeOn(vent);
    }

    public override void TickInterval(int delta)
    {
        if (density > DensityDissipationThreshold)   //very low density smoke clouds eventually dissipate on their own
        {
            destroyTick += delta;
            float dissipation = Mathf.Max(0.001f, density * 0.00001f) * delta;

            if (density > 300 && Rand.Range(0, (int)(MaxDensity)) < dissipation)
            {
                FilthMaker.TryMakeFilth(Position, Map, ThingDefOf.Filth_Ash, 1, FilthSourceFlags.None);
            }

            density -= dissipation;
        }

        if (this.IsHashIntervalTick(120, delta))
        {
            DangerTracker?.Notify_SmokeAt(Position, density / MaxDensity);
        }

        base.TickInterval(delta);
    }

    public void DoSpreadToAdjacentCells()
    {
        if (this.IsHashIntervalTick(UpdateIntervalTicks))
        {
            for (int i = 0; i < _transfers.Count; i++)
            {
                DensityTransfer transfer = _transfers[i];
                Smoke target = transfer.Target ?? (Smoke)GenSpawn.Spawn(CE_ThingDefOf.Gas_BlackSmoke, transfer.Position, Map);
                TransferDensityTo(target, transfer.Amount);
            }

            _transfers.Clear();
        }
    }

    public void ParallelTick()
    {
        if (this.IsHashIntervalTick(UpdateIntervalTicks))
        {
            if (!CanMoveTo(Position))   //cloud is in inaccessible cell, probably a recently closed door or vent. Spread to nearby cells and delete.
            {
                destroyTick = 0;
            }
            if (!Position.Roofed(Map))
            {
                UpdateDensityBy(-60);
            }
            CalcSpreadToAdjacentCells();
        }
    }

    public void ApplyHediffs(Pawn pawn)
    {
        var baseTargetSeverity = Mathf.Pow(density / LethalAirPPM, 1.25f);
        var baseSeverityRate = InhalationPerSec * density / MaxDensity;

        if (pawn.RaceProps.FleshType == FleshTypeDefOf.Insectoid)
        {
            if (density > MaxDensity * BugConfusePercent)
            {
                pawn.mindState.mentalStateHandler.TryStartMentalState(CE_MentalStateDefOf.WanderConfused);
            }

            return;
        }

        if (pawn.RaceProps.Humanlike && !pawn.IsSubhuman)
        {
            pawn.TryGetComp<CompTacticalManager>()?.GetTacticalComp<CompGasMask>()?.Notify_ShouldEquipGasMask(false);
        }
        var sensitivity = pawn.GetStatValue(CE_StatDefOf.SmokeSensitivity);
        var breathing = PawnCapacityUtility.CalculateCapacityLevel(pawn.health.hediffSet, PawnCapacityDefOf.Breathing);
        float curSeverity = pawn.health.hediffSet.GetFirstHediffOfDef(CE_HediffDefOf.SmokeInhalation, false)?.Severity ?? 0f;


        if (breathing < 0.01f)
        {
            breathing = 0.01f;
        }
        var targetSeverity = sensitivity / breathing * baseTargetSeverity;
        if (targetSeverity > 1.5f)
        {
            targetSeverity = 1.5f;
        }

        var severityDelta = targetSeverity - curSeverity;

        bool downed = pawn.Downed;
        bool awake = pawn.Awake();


        var severityRate = baseSeverityRate * sensitivity / breathing * Mathf.Pow(severityDelta, 1.5f);

        if (downed)
        {
            severityRate /= 100;
        }

        if (!awake)
        {
            severityRate /= 2;
            if (curSeverity > 0.1)
            {
                RestUtility.WakeUp(pawn);
            }
        }

        if (severityRate > 0 && severityDelta > 0)
        {
            HealthUtility.AdjustSeverity(pawn, CE_HediffDefOf.SmokeInhalation, severityRate);
        }
    }

    private void CalcSpreadToAdjacentCells()
    {
        if (density < MinSpreadDensity)
        {
            return;
        }

        Map map = Map;
        IntVec3 position = Position;
        float curDensity = density;

        foreach (IntVec3 cardinal in GenAdj.CardinalDirections.InRandomOrder())
        {
            IntVec3 freeCell = position + cardinal;

            if (!CanMoveTo(freeCell))
            {
                continue;
            }

            float transferred;
            if (freeCell.GetGas(map) is Smoke existingSmoke)
            {
                transferred = (curDensity - existingSmoke.density) / 2;
                _transfers.Add(new DensityTransfer
                {
                    Target = existingSmoke,
                    Amount = transferred
                });
            }
            else
            {
                transferred = curDensity / 2;
                _transfers.Add(new DensityTransfer
                {
                    Position = freeCell,
                    Amount = transferred
                });
            }

            curDensity -= transferred;
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
