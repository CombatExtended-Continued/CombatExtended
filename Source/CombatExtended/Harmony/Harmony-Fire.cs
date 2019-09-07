using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Reflection.Emit;
using Harmony;
using Verse;
using RimWorld;
using UnityEngine;

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
                    code.operand = 0.6f;
                }
                yield return code;
            }
        }
    }

    [HarmonyPatch(typeof(Fire), "TrySpread")]
    internal static class Harmony_Fire_TrySpread
    {
        private const float SpreadFarBaseChance = 0.02f;
        private static SimpleCurve _angleCurveWide;
        private static SimpleCurve _angleCurveNarrow;

        private static IntVec3 GetRandWindShift(Map map, bool spreadFar)
        {
            if (_angleCurveWide == null)
            {
                _angleCurveWide = new SimpleCurve
                {
                    {0, 360},
                    {3, 210},
                    {6, 90},
                    {9, 30},
                    {999, 1}
                };
            }
            if (_angleCurveNarrow == null)
            {
                _angleCurveNarrow = new SimpleCurve
                {
                    {0, 360},
                    {3, 120},
                    {6, 30},
                    {9, 10},
                    {999, 1}
                };
            }

            var tracker = map.GetComponent<WeatherTracker>();
            var angleDelta = spreadFar
                ? _angleCurveNarrow.Evaluate(tracker.WindStrength)
                : _angleCurveWide.Evaluate(tracker.WindStrength);
            angleDelta *= 0.5f;
            var angle = Rand.Range(-angleDelta, angleDelta);
            var vec = tracker.WindDirection.RotatedBy(angle);
            if (spreadFar)
                vec *= Rand.Range(1, Mathf.Max(2, tracker.WindStrength));

            return vec.ToIntVec3();
        }

        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var delete = false;
            var passedRadPattern = false;
            var codes = new List<CodeInstruction>();
            var getComponentMethodInfo = AccessTools.Method(typeof(Map), nameof(Map.GetComponent), new Type[] { }).MakeGenericMethod(typeof(WeatherTracker));
            foreach (var code in instructions)
            {
                if (delete)
                {
                    delete = code.opcode != OpCodes.Ldobj;
                    continue;
                }

                if (code.operand == AccessTools.Field(typeof(GenRadial), nameof(GenRadial.ManualRadialPattern)))
                {
                    codes.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    codes.Add(new CodeInstruction(OpCodes.Call, AccessTools.Property(typeof(Fire), nameof(Fire.Map)).GetGetMethod()));
                    codes.Add(passedRadPattern
                        ? new CodeInstruction(OpCodes.Ldc_I4_1)
                        : new CodeInstruction(OpCodes.Ldc_I4_0));
                    codes.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Harmony_Fire_TrySpread), nameof(GetRandWindShift))));

                    passedRadPattern = true;
                    delete = true;
                    continue;
                }

                // Replace spread chance with our wind-dependent one
                if (code.opcode == OpCodes.Ldc_R4 && code.operand is float operand && operand == 0.8f)
                {
                    code.operand = 1f;

                    codes.Add(code);
                    codes.Add(new CodeInstruction(OpCodes.Ldc_R4, SpreadFarBaseChance));

                    codes.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    codes.Add(new CodeInstruction(OpCodes.Call, AccessTools.Property(typeof(Fire), nameof(Fire.Map)).GetGetMethod()));

                    codes.Add(new CodeInstruction(OpCodes.Call, getComponentMethodInfo));
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
        private const int MinSpreadTicks = 1;

        internal static bool Prefix(Fire __instance, ref float __result)
        {
            __result = BaseSpreadRate - (__instance.fireSize - 1) * SpreadSizeAdjust;
            var windSpeed = __instance.Map.GetComponent<WeatherTracker>().WindStrength;
            __result /= Mathf.Max(1, windSpeed);

            if (__result > MinSpreadTicks)
            {
                __result = MinSpreadTicks;
            }

            return false;
        }
    }
}
