using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CombatExtended.AI;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE
{
    internal static class Harmony_GasUtility
    {
        /// <summary>
        /// Transpile <see cref="GasUtility.DoAirbornePawnToxicDamage"/> to tell AI pawns to wear mask
        /// when exposed to toxic gas
        /// </summary>
        [HarmonyPatch(typeof(GasUtility), nameof(GasUtility.PawnGasEffectsTickInterval))]
        static class Patch_PawnGasEffectsTickInterval
        {

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                List<CodeInstruction> codes = instructions.ToList();

                MethodInfo firstInjectionSite = AccessTools.Method(typeof(GasUtility), nameof(GasUtility.ShouldGetGasExposureHediff));
                MethodInfo additionalMethod = AccessTools.Method(typeof(Harmony_GasUtility), nameof(Harmony_GasUtility.TryNotify_ShouldEquipGasMask));
                FieldInfo secondInjectionSite = AccessTools.Field(typeof(HediffDefOf), nameof(HediffDefOf.LungRotExposure));
                bool firstInjectionFound = false;
                bool secondInjectionFound = false;
                for (int i = 0; i < codes.Count - 2; i++)
                {
                    // Injecting this before the target method, so that pawns can put the gas mask on to avoid the debilitating exposure hediff.
                    if (codes[i].Calls(firstInjectionSite) && !firstInjectionFound)
                    {
                        codes.Insert(i, new CodeInstruction(OpCodes.Call, additionalMethod));
                        codes.Insert(i, new CodeInstruction(OpCodes.Dup));
                        firstInjectionFound = true;
                    }
                    if (codes[i].opcode == OpCodes.Ldarg_0 && codes[i + 2].operand is FieldInfo fieldInfo && fieldInfo == secondInjectionSite && !secondInjectionFound)
                    {
                        codes.Insert(i + 1 , new CodeInstruction(OpCodes.Call, additionalMethod));
                        codes.Insert(i + 1 , new CodeInstruction(OpCodes.Dup));
                        secondInjectionFound = true;
                    }
                }
                return codes;
            }
        }

        public static void TryNotify_ShouldEquipGasMask(Pawn pawn)
        {
            if (pawn.RaceProps.Humanlike && !pawn.IsSubhuman)
            {
                pawn.TryGetComp<CompTacticalManager>()?.GetTacticalComp<CompGasMask>()?.Notify_ShouldEquipGasMask();
            }
        }
    }
}
