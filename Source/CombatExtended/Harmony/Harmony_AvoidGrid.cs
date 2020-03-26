using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Verse.AI;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(AvoidGrid), "PrintAvoidGridAroundTurret")]
    internal static class Harmony_AvoidGrid_PrintAvoidGridAroundTurret
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var code in instructions)
            {
                if (code.opcode == OpCodes.Ldc_I4_S && (sbyte) code.operand == 45)
                    code.operand = 8;
                yield return code;
            }
        }
    }
}