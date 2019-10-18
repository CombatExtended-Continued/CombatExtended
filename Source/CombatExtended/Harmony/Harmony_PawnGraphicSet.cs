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
    [HarmonyPatch(typeof(PawnGraphicSet), "MatsBodyBaseAt")]
    internal static class Harmony_PawnGraphicSet
    {
        private static bool RenderSpecial(ApparelLayerDef layer)
        {
            return (layer.GetModExtension<ApparelLayerExtension>()?.IsHeadwear ?? false)
                   || layer.drawOrder > ApparelLayerDefOf.Shell.drawOrder;
        }

        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var write = false;

            foreach (var code in instructions)
            {
                if (write)
                {
                    write = false;
                    code.opcode = OpCodes.Brtrue;
                }

                if (code.opcode == OpCodes.Ldsfld && code.operand == AccessTools.Field(typeof(ApparelLayerDefOf), nameof(ApparelLayerDefOf.Overhead)))
                {
                    write = true;
                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Harmony_PawnGraphicSet), nameof(RenderSpecial)));
                }
                else
                {
                    yield return code;
                }
            }
        }

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
