using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(AvoidGrid), "PrintAvoidGridAroundTurret")]
    internal static class Harmony_AvoidGrid_PrintAvoidGridAroundTurret
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool foundInjection = false;
            foreach (var code in instructions)
            {
                if (code.opcode == OpCodes.Ldc_I4_S && (sbyte)code.operand == 45)
                {
                    code.operand = 8;
                    foundInjection = true;
                }
                yield return code;
            }
            if (!foundInjection)
            {
                Log.Error($"Combat Extended :: Failed to find injection point when applying Patch: {HarmonyBase.GetClassName(MethodBase.GetCurrentMethod()?.DeclaringType)}");
            }
        }
    }
}
