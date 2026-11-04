using System.Collections.Generic;
using CombatExtended.AI;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended;

public class CEGasGrid : MapComponent
{
    public const int UpdateIntervalTicks = 60;

    private const float MinBreathingLevel = 0.01f;
    private const float MaxTargetSeverity = 1.5f;
    private const float DownedGainDivisor = 100f;
    private const float WakeUpSeverity = 0.1f;

    private readonly List<GasLayer> layers = [];
    private readonly Dictionary<GasDef, GasLayer> layersByDef = [];
    private readonly int[] cachedAtTick;
    private readonly bool[] cachedCanMoveTo;

    public CEGasGrid(Map map) : base(map)
    {
        cachedAtTick = new int[map.cellIndices.NumGridCells];
        cachedCanMoveTo = new bool[map.cellIndices.NumGridCells];
        foreach (GasDef def in DefDatabase<GasDef>.AllDefsListForReading)
        {
            var layer = new GasLayer(this, map, def);
            layers.Add(layer);
            layersByDef[def] = layer;
        }
    }

    public bool AnyGas
    {
        get
        {
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i].HasActiveCells)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public IReadOnlyList<GasLayer> Layers => layers;

    public GasLayer LayerFor(GasDef def) => layersByDef[def];

    public float DensityAt(IntVec3 cell, GasDef def) => layersByDef[def].DensityAt(cell);

    public void AddDensity(IntVec3 cell, GasDef def, float amount) => layersByDef[def].AddDensity(cell, amount);

    public override void ExposeData()
    {
        for (int i = 0; i < layers.Count; i++)
        {
            layers[i].ExposeData();
        }
    }

    public override void MapComponentTick()
    {
        int ticks = GenTicks.TicksGame;
        for (int i = 0; i < layers.Count; i++)
        {
            layers[i].Tick(ticks);
        }
    }


    public bool CanMoveTo(int index)
    {
        int currentStamp = GenTicks.TicksGame + 1;   // +1 so the zero-filled stamp array never counts as cached
        if (cachedAtTick[index] == currentStamp)
        {
            return cachedCanMoveTo[index];
        }

        Building edifice = map.edificeGrid.InnerArray[index];
        bool isSolid = edifice != null && edifice.def.Fillage == FillCategory.Full;
        bool isOpening = edifice is Building_Door { Open: true } ||
                         edifice is Building_Vent vent && FlickUtility.WantsToBeOn(vent);
        bool canMoveTo = !isSolid || isOpening;

        cachedAtTick[index] = currentStamp;
        cachedCanMoveTo[index] = canMoveTo;
        return canMoveTo;
    }


    public static void ApplyHediff(Pawn pawn, GasDef def, float cellDensity)
    {
        if (pawn.RaceProps.FleshType == FleshTypeDefOf.Insectoid)
        {
            ConfuseInsectoid(pawn, def, cellDensity);
            if (def.insectoidsOnlyConfused)
            {
                return;
            }
        }

        if (def.notifyGasMask && pawn.RaceProps.Humanlike && !pawn.IsSubhuman)
        {
            pawn.TryGetComp<CompTacticalManager>()?.GetTacticalComp<CompGasMask>()?.Notify_ShouldEquipGasMask(false);
        }

        if (def.hediff != null)
        {
            BuildUpHediff(pawn, def, cellDensity);
        }
    }

    private static void ConfuseInsectoid(Pawn pawn, GasDef def, float cellDensity)
    {
        if (!def.confusesInsectoids || cellDensity <= def.maxDensity * def.bugConfusePercent)
        {
            return;
        }

        MentalStateHandler handler = pawn.mindState.mentalStateHandler;
        if (handler.CurStateDef != CE_MentalStateDefOf.WanderConfused)
        {
            handler.TryStartMentalState(CE_MentalStateDefOf.WanderConfused);
        }
    }


    private static void BuildUpHediff(Pawn pawn, GasDef def, float cellDensity)
    {
        float sensitivity = def.sensitivityStat != null ? pawn.GetStatValue(def.sensitivityStat) : 1f;
        if (sensitivity <= 0f)
        {
            return;
        }
        float breathing = Mathf.Max(MinBreathingLevel, pawn.health.capacities.GetLevel(PawnCapacityDefOf.Breathing));
        float vulnerability = sensitivity / breathing;

        float targetSeverity = Mathf.Min(MaxTargetSeverity, vulnerability * Mathf.Pow(cellDensity / def.lethalDensity, 1.25f));
        float currentSeverity = pawn.health.hediffSet.GetFirstHediffOfDef(def.hediff, false)?.Severity ?? 0f;
        float severityLeftToGain = targetSeverity - currentSeverity;
        if (severityLeftToGain <= 0f)
        {
            return;
        }

        float fullDensityGainPerInterval = def.inhalationPerSecond * UpdateIntervalTicks / GenTicks.TicksPerRealSecond;
        float gainPerInterval = fullDensityGainPerInterval * (cellDensity / def.maxDensity) * vulnerability * Mathf.Pow(severityLeftToGain, 1.5f);

        if (pawn.Downed)
        {
            gainPerInterval /= DownedGainDivisor;
        }

        if (!pawn.Awake())
        {
            gainPerInterval /= 2;
            bool alreadyBadlyAffected = currentSeverity > WakeUpSeverity;
            if (alreadyBadlyAffected)
            {
                RestUtility.WakeUp(pawn);
            }
        }

        if (gainPerInterval > 0f)
        {
            HealthUtility.AdjustSeverity(pawn, def.hediff, gainPerInterval);
        }
    }
}
