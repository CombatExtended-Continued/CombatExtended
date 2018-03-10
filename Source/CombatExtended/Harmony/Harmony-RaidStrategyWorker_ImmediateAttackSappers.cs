using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using RimWorld;
using Verse;
using UnityEngine;
using Harmony;

namespace CombatExtended.Harmony
{
    [HarmonyPatch(typeof(RaidStrategyWorker_ImmediateAttackSappers), "CanBeSapper")]
    public static class Harmony_RaidStrategyWorker_ImmediateAttackSappers_CanBeSapper
    {
        public static void Postfix(ref bool __result, PawnKindDef kind)
        {
            if (!__result)
            {
                __result = !kind.weaponTags.NullOrEmpty() && kind.weaponTags.Contains("CE_GrenadeNeolithic");
            }
        }
    }

    [HarmonyPatch(typeof(RaidStrategyWorker_ImmediateAttackSappers), "CanUseWith")]
    static class Harmony_RaidStrategyWorker_ImmediateAttackSappers_CanUseWith
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            for (int i = 0; i < codes.Count - 1; i++)
            {
                if (codes[i].operand as FieldInfo != null && (codes[i].operand as FieldInfo) == AccessTools.Field(typeof(FactionDef), "techLevel"))
                {
                    codes[i + 1].opcode = OpCodes.Ldc_I4_0;
                    return codes;
                }
            }
            Log.Error("CE failed to transpile RaidStrategyWorker_ImmediateAttackSappers.CanUseWith() :: failed to find tech level check");
            return instructions;
        }
    }
}
