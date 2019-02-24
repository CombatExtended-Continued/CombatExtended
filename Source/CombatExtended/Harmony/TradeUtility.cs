using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using Harmony;
using Verse;

namespace CombatExtended.Harmony
{
    [HarmonyPatch(typeof(RimWorld.TradeUtility), "GetPricePlayerBuy")]
    internal static class TradeUtility
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && (instruction.operand?.Equals(0.5f) ?? false))
                {
                    instruction.operand = 0.01f;
                }

                yield return instruction;
            }
        }
    }
}