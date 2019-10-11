using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using Verse;

namespace CombatExtended.Harmony
{
    class Harmony_PawnGraphicSet
    {
        ///// <summary>
        ///// Patches renderer to treat Shell as a regular layer that obeys drawOrder, so apparel like backpacks can be drawn over it.
        ///// As Shell layer is normally drawn later in Harmony_PawnRenderer.RenderPawnInternal(),
        ///// that method is also patched in Harmony_PawnRenderer to prevent Shell-layer apparel from being drawn twice.
        ///// </summary>
        //[HarmonyPatch(typeof(PawnGraphicSet), "MatsBodyBaseAt")]
        //class RenderShellAsNormalLayer
        //{
        //    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        //    {
        //        foreach (var code in instructions)
        //        {
        //            if (code.operand == AccessTools.Field(typeof(ApparelLayerDefOf), "Shell"))
        //            {
        //                code.operand = AccessTools.Field(typeof(ApparelLayerDefOf), "Overhead");
        //            }
        //            yield return code;
        //        }
        //    }
        //}
    }
}
