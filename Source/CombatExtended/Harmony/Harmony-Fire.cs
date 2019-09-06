using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Reflection.Emit;
using Harmony;
using Verse;
using RimWorld;

namespace CombatExtended.Harmony
{
    [HarmonyPatch(typeof(Fire), "DoFireDamage")]
    internal static class Harmony_Fire_DoFireDamage
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.opcode == OpCodes.Ldc_R4 && code.operand is float && (float)code.operand == 150f)
                {
                    code.operand = 300f;
                }
                yield return code;
            }
        }
    }

    [HarmonyPatch(typeof(Fire), "Tick")]
    internal static class Harmony_Fire_Tick
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.opcode == OpCodes.Ldc_R4 && code.operand is float && (float)code.operand == 1f)
                {
                    code.operand = 0.1f;
                }
                yield return code;
            }
        }
    }

    [HarmonyPatch(typeof(Fire), "TrySpread")]
    internal static class Harmony_Fire_TrySpread
    {
        private const float SpreadFarBaseChance = 0.025f;

        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>();
            foreach (CodeInstruction code in instructions)
            {
                if (code.opcode == OpCodes.Ldc_R4 && code.operand is float operand && operand == 0.8f)
                {
                    code.operand = 1f;

                    codes.Add(code);
                    codes.Add(new CodeInstruction(OpCodes.Ldc_R4, SpreadFarBaseChance));

                    codes.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    codes.Add(new CodeInstruction(OpCodes.Call, AccessTools.Property(typeof(Fire), nameof(Fire.Map)).GetGetMethod()));

                    var methodInfo = AccessTools.Method(typeof(Map), nameof(Map.GetComponent), new Type[] { }).MakeGenericMethod(typeof(WeatherTracker));
                    codes.Add(new CodeInstruction(OpCodes.Call, methodInfo));
                    codes.Add(new CodeInstruction(OpCodes.Call, AccessTools.Property(typeof(WeatherTracker), nameof(WeatherTracker.WindStrength)).GetGetMethod()));

                    codes.Add(new CodeInstruction(OpCodes.Mul));
                    codes.Add(new CodeInstruction(OpCodes.Sub));
                    continue;
                }

                codes.Add(code);
            }

            return codes;
        }
    }

    [HarmonyPatch(typeof(Fire), "get_SpreadInterval")]
    internal static class Harmony_Fire_SpreadInterval
    {
        private const float BaseSpreadRate = 94f;
        private const float SpreadSizeAdjust = 85f;
        private const int MinSpreadTicks = 30;

        internal static bool Prefix(Fire __instance, ref float __result)
        {
            __result = BaseSpreadRate - (__instance.fireSize - 1) * SpreadSizeAdjust;
            if (__result > MinSpreadTicks)
            {
                __result = MinSpreadTicks;
            }

            return false;
        }
    }
}
