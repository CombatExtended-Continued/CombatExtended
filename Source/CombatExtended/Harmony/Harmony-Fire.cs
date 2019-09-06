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
                    code.operand = 0.1f;
                }
                yield return code;
            }
        }
    }

    [HarmonyPatch(typeof(Fire), "TrySpread")]
    internal static class Harmony_Fire_TrySpread
    {
        private const float SpreadFarBaseChance = 0.02f;
        private static SimpleCurve _angleCurve;

        private static IntVec3 GetRandWindShift(Map map)
        {
            if (_angleCurve == null)
            {
                _angleCurve = new SimpleCurve
                {
                    {0, 360},
                    {3, 150},
                    {6, 30},
                    {9, 10},
                    {999, 1}
                };
            }

            var tracker = map.GetComponent<WeatherTracker>();
            var angleDelta = _angleCurve.Evaluate(tracker.WindStrength) * 0.5f;
            var angle = Rand.Range(-angleDelta, angleDelta);
            var vec = tracker.WindDirection.RotatedBy(angle) * Rand.Range(1, Mathf.Max(2, tracker.WindStrength));

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
                    if (passedRadPattern)
                    {
                        delete = true;
                        codes.Add(new CodeInstruction(OpCodes.Ldarg_0));
                        codes.Add(new CodeInstruction(OpCodes.Call, AccessTools.Property(typeof(Fire), nameof(Fire.Map)).GetGetMethod()));
                        codes.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Harmony_Fire_TrySpread), nameof(GetRandWindShift))));
                        continue;
                    }

                    passedRadPattern = true;
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
        private const float BaseSpreadRate = 188f;
        private const float SpreadSizeAdjust = 170f;
        private const int MinSpreadTicks = 10;

        internal static bool Prefix(Fire __instance, ref float __result)
        {
            __result = BaseSpreadRate - (__instance.fireSize - 1) * SpreadSizeAdjust;
            var windSpeed = __instance.Map.GetComponent<WeatherTracker>().WindStrength;
            __result /= Mathf.Max(1, Mathf.Sqrt(windSpeed));

            if (__result > MinSpreadTicks)
            {
                __result = MinSpreadTicks;
            }

            return false;
        }
    }
}
