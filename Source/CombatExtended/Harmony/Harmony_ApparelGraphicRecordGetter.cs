using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(ApparelGraphicRecordGetter), "TryGetGraphicApparel")]
    internal static class Harmony_ApparelGraphicRecordGetter
    {
        private static bool IsHeadwear(ApparelLayerDef layer)
        {
            return layer.GetModExtension<ApparelLayerExtension>()?.IsHeadwear ?? false;
        }

        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var write = false;

            foreach (var code in instructions)
            {
                if (write)
                {
                    write = false;
                    code.opcode = OpCodes.Brfalse;
                }

                if (code.opcode == OpCodes.Ldsfld && code.operand == AccessTools.Field(typeof(ApparelLayerDefOf), nameof(ApparelLayerDefOf.Overhead)))
                {
                    write = true;
                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Harmony_ApparelGraphicRecordGetter), nameof(IsHeadwear)));
                }
                else
                {
                    yield return code;
                }
            }
        }
    }
}