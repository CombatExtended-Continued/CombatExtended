using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using Harmony;
using RimWorld;

namespace CombatExtended.Harmony;
[HarmonyPatch(typeof(WeatherDecider), "CurrentWeatherCommonality")]
internal static class Harmony_WeatherDecider
{
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var code in instructions)
        {
            if (code.opcode == OpCodes.Ldc_R4 && code.operand is float operand && operand == 15f)
            {
                code.operand = 15f;
            }
            yield return code;
        }
    }
}