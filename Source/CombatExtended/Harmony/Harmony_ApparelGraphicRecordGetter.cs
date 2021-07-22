using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE
{
    /*
     *  If all apparel worn on pawns is the drop image of that apparel,
     *      PLEASE change "code.opcode = OpCodes.Brtrue"
     *                 to "code.opcode = OpCodes.Brfalse"
     *      
     *      
     *  This is due to the IL generated upon compiling:
     *  
     *  - sometimes, the generated IL jumps to the "else" condition (draw as worn apparel rather than as an item)
     *      when the provided check (RenderSpecial) is TRUE
     *      
     *  - at other times, it jumps when the check is FALSE
     */

    [HarmonyPatch(typeof(ApparelGraphicRecordGetter), "TryGetGraphicApparel")]
    internal static class Harmony_ApparelGraphicRecordGetter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                    code.opcode = OpCodes.Brtrue;   //OR try Brfalse
                }

                if (code.opcode == OpCodes.Ldsfld && ReferenceEquals(code.operand, AccessTools.Field(typeof(ApparelLayerDefOf), nameof(ApparelLayerDefOf.Overhead))))
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
