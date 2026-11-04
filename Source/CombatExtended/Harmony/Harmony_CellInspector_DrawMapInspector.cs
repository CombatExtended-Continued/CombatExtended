using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace CombatExtended.HarmonyCE;

[HarmonyPatch(typeof(CellInspectorDrawer), "DrawMapInspector")]
internal static class Harmony_CellInspector_DrawMapInspector
{
    internal static void Postfix()
    {
        Map map = Find.CurrentMap;
        IntVec3 cell = UI.MouseCell();
        if (map == null || !cell.InBounds(map))
        {
            return;
        }

        IReadOnlyList<GasLayer> layers = map.GetComponent<CEGasGrid>().Layers;
        for (int i = 0; i < layers.Count; i++)
        {
            float density = layers[i].DensityAt(cell);
            if (density <= 0f)
            {
                continue;
            }
            GasDef def = layers[i].Def;
            CellInspectorDrawer.DrawRow(def.LabelCap, DensityText(def, density));
        }
    }

    /// <summary>A density as a percent of the gas's maximum or in ppm, following the setting.</summary>
    internal static string DensityText(GasDef def, float density) => Controller.settings.SmokeShowPercent ? (density / def.maxDensity).ToStringPercent("0.#") : Mathf.RoundToInt(density * def.ppmPerDensity) + " ppm";
}
