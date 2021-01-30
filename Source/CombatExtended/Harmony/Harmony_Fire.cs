using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(Fire), "DoFireDamage")]
    internal static class Harmony_Fire_DoFireDamage
    {
        private static void ApplySizeMult(Pawn pawn, ref float damage)
        {
            damage *= pawn.BodySize;
        }

        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.opcode == OpCodes.Ldc_R4 && code.operand is float && (float)code.operand == 150f)
                {
                    code.operand = 300f;
                }

                if (ReferenceEquals(code.operand, AccessTools.Field(typeof(RulePackDefOf), nameof(RulePackDefOf.DamageEvent_Fire))))
                {
                    yield return new CodeInstruction(OpCodes.Ldloca, 0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_Fire_DoFireDamage), nameof(ApplySizeMult)));
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                }

                yield return code;
            }
        }
    }

    [HarmonyPatch(typeof(Fire), "SpawnSmokeParticles")]
    internal static class Harmony_Fire_SpawnSmokeParticles
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                //Log.Message($"OpCode {code.opcode}, operand {code.operand}, type {code.operand.GetType()}");
                if (code.opcode == OpCodes.Ldc_I4_S && code.operand is sbyte && (sbyte)code.operand == 15)
                {
                    yield return new CodeInstruction(OpCodes.Ldc_I4, 1500);
                }
                else
                {
                    yield return code;
                }
            }
        }

        internal static void Postfix(Fire __instance)
        {
        }
    }

    [HarmonyPatch(typeof(Fire), "Tick")]
    internal static class Harmony_Fire_Tick
    {
        private const float SmokeDensityPerInterval = 900f;

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

        internal static void Postfix(Fire __instance)
        {
            if (__instance.Spawned
                && Controller.settings.SmokeEffects
                && __instance.IsHashIntervalTick(Smoke.UpdateIntervalTicks)
                && __instance.Position.Roofed(__instance.Map))
            {
                if (__instance.Position.GetGas(__instance.Map) is Smoke existingSmoke)
                {
                    existingSmoke.UpdateDensityBy(SmokeDensityPerInterval);
                }
                else
                {
                    var newSmoke = (Smoke)GenSpawn.Spawn(CE_ThingDefOf.Gas_BlackSmoke, __instance.Position, __instance.Map);
                    newSmoke.UpdateDensityBy(SmokeDensityPerInterval);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Fire), "DoComplexCalcs")]
    internal static class Harmony_Fire_DoComplexCalcs
    {
        private static float GetWindGrowthAdjust(Fire fire)
        {
            var tracker = fire.Map.GetComponent<WeatherTracker>();
            return FireSpread.values.baseGrowthPerTick * (1 + Mathf.Sqrt(tracker.GetWindStrengthAt(fire.Position)) * FireSpread.values.windSpeedMultiplier);
        }

        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.opcode == OpCodes.Ldc_R4 && code.operand is float && (float)code.operand == 0.00055f)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_Fire_DoComplexCalcs), nameof(GetWindGrowthAdjust)));
                }
                else
                {
                    yield return code;
                }
            }
        }
        internal static void Postfix(Fire __instance)
        {
            if (__instance.Spawned && __instance.parent is Pawn pawn)
            {
                __instance.fireSize -= pawn.GetStatValue(StatDefOf.Flammability, true) * 150 * 0.00055f;
                float mult = 0.0f;
                var apparel = pawn.apparel?.WornApparel ?? null;
                if (apparel==null)
                {
                    return;
                }
                var parts = pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Outside);
                float coverageAbs = 0;
                foreach (var part in parts)
                {
                    coverageAbs += part.coverageAbs;
                    float combustibility = 1.0f;
                    foreach (Apparel item in apparel)
                    {
                        if (item.def.apparel.CoversBodyPart(part))
                        {
                            combustibility = ((item.GetStatValue(StatDefOf.Flammability, true)) - 0.1f) / 0.50f;
                            break;
                        }
                    }
                    mult += part.coverageAbs * combustibility;
                }
                mult /= coverageAbs;
                __instance.fireSize += 0.00055f * mult * 1.5f;
                if (__instance.fireSize < Fire.MinFireSize) {
                    __instance.Destroy(DestroyMode.Vanish);
                }
            }
        }

    }

    [HarmonyPatch(typeof(Fire), "TrySpread")]
    internal static class Harmony_Fire_TrySpread
    {
        private static SimpleCurve _angleCurveWide;
        private static SimpleCurve _angleCurveNarrow;

        private static float GetWindMult(Fire fire)
        {
            var tracker = fire.Map.GetComponent<WeatherTracker>();
            float balancedWindMultiplier = Mathf.Sqrt(tracker.GetWindStrengthAt(fire.Position)) * FireSpread.values.windSpeedMultiplier;
            return FireSpread.values.spreadFarBaseChance * Mathf.Max(1, balancedWindMultiplier);
        }

        private static IntVec3 GetRandWindShift(Fire fire, bool spreadFar)
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

            var tracker = fire.Map.GetComponent<WeatherTracker>();
            var angleDelta = spreadFar
                ? _angleCurveNarrow.Evaluate(tracker.GetWindStrengthAt(fire.Position) * FireSpread.values.windSpeedMultiplier)
                : _angleCurveWide.Evaluate(tracker.GetWindStrengthAt(fire.Position) * FireSpread.values.windSpeedMultiplier);
            angleDelta *= 0.5f;
            var angle = Rand.Range(-angleDelta, angleDelta);
            var vec = tracker.WindDirection.RotatedBy(angle);
            if (spreadFar)
                vec *= Rand.Range(1, Mathf.Max(2, tracker.GetWindStrengthAt(fire.Position) * FireSpread.values.windSpeedMultiplier));

            return vec.ToIntVec3();
        }

        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var delete = false;
            var passedRadPattern = false;
            var codes = new List<CodeInstruction>();

            foreach (var code in instructions)
            {
                if (delete)
                {
                    delete = code.opcode != OpCodes.Ldelem;

                    continue;
                }

                if (ReferenceEquals(code.operand, AccessTools.Field(typeof(GenRadial), nameof(GenRadial.ManualRadialPattern))))
                {
                    codes.Add(new CodeInstruction(OpCodes.Ldarg_0));
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

                    codes.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    codes.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_Fire_TrySpread), nameof(GetWindMult))));

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
        internal static bool Prefix(Fire __instance, ref float __result)
        {
            __result = FireSpread.values.baseSpreadTicks - (__instance.fireSize * FireSpread.values.fireSizeMultiplier);
            float windSpeed = __instance.Map.GetComponent<WeatherTracker>().GetWindStrengthAt(__instance.PositionHeld) * FireSpread.values.windSpeedMultiplier;
            __result /= Mathf.Max(1, Mathf.Sqrt(windSpeed));

            if (__result < FireSpread.values.minSpreadTicks)
            {
                __result = FireSpread.values.minSpreadTicks;
            }

            return false;
        }
    }

}
