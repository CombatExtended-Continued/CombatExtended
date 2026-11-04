using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace CombatExtended.HarmonyCE;

/// Shows the thickness of each of our gases in the bottom-left mouseover readout, the same way vanilla shows its gases.
[HarmonyPatch(typeof(MouseoverReadout), nameof(MouseoverReadout.MouseoverReadoutOnGUI))]
internal static class Harmony_MouseoverReadout_Smoke
{
    private const float LineHeight = 19f;

    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        // Look up the vanilla gas grid method.
        MethodInfo anyGasAt = AccessTools.Method(typeof(Verse.GasGrid), nameof(GasGrid.AnyGasAt), [typeof(IntVec3)]);
        MethodInfo drawGas = AccessTools.Method(typeof(MouseoverReadout), "DrawGas");
        MethodInfo drawOurGases = AccessTools.Method(typeof(Harmony_MouseoverReadout_Smoke), nameof(DrawOurGases));

        int anyGasIndex = codes.FindIndex(code => code.Calls(anyGasAt));
        int drawGasIndex = codes.FindIndex(code => code.Calls(drawGas));
        if (anyGasIndex < 3 || drawGasIndex < 1)
        {
            Log.Error($"Combat Extended :: Failed to find injection point when applying Patch: {HarmonyBase.GetClassName(MethodBase.GetCurrentMethod()?.DeclaringType)}");
            return codes;
        }

        // The vanilla gas block starts with Find.CurrentMap.gasGrid.AnyGasAt(cell); add our line right before it.
        // The cell local is the argument to AnyGasAt and the running y offset is the ref argument of DrawGas.
        int insertAt = anyGasIndex - 3;
        var injected = new List<CodeInstruction>
        {
            codes[anyGasIndex - 1].Clone(),
            codes[drawGasIndex - 1].Clone(),
            new CodeInstruction(OpCodes.Call, drawOurGases)
        };
        codes[insertAt].MoveLabelsTo(injected[0]);
        codes.InsertRange(insertAt, injected);
        return codes;
    }

    private static void DrawOurGases(IntVec3 cell, ref float curYOffset)
    {
        CEGasGrid grid = Find.CurrentMap.GetComponent<CEGasGrid>();
        if (grid is not { AnyGas: true })
        {
            return;
        }

        IReadOnlyList<GasLayer> layers = grid.Layers;
        for (int i = 0; i < layers.Count; i++)
        {
            float density = layers[i].DensityAt(cell);
            if (density <= 0f)
            {
                continue;
            }

            GasDef def = layers[i].Def;
            string label = def.LabelCap + " " + Harmony_CellInspector_DrawMapInspector.DensityText(def, density);
            Widgets.Label(new Rect(MouseoverReadout.BotLeft.x, UI.screenHeight - MouseoverReadout.BotLeft.y - curYOffset, 999f, 999f), label);
            curYOffset += LineHeight;
        }
    }
}
