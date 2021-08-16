using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using Verse;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(PawnGraphicSet), "MatsBodyBaseAt")]
    internal static class Harmony_PawnGraphicSet
    {
        private static bool RenderSpecial(ApparelLayerDef layer)
        {
            return (layer.GetModExtension<ApparelLayerExtension>()?.IsHeadwear ?? false) || layer.drawOrder > ApparelLayerDefOf.Shell.drawOrder;
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

                if (code.opcode == OpCodes.Ldsfld && ReferenceEquals(code.operand, AccessTools.Field(typeof(ApparelLayerDefOf), nameof(ApparelLayerDefOf.Overhead))))
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
    }
}
