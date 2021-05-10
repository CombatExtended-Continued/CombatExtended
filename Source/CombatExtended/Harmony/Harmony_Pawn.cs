using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Verse;
using Verse.AI;

namespace CombatExtended.HarmonyCE
{
    //Make non-lethal weapons useable by non-violent pawns (general)
    [HarmonyPatch]
    class Harmony_NonViolentNonLethal_Patch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Pawn), nameof(Pawn.TryStartAttack));
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.MethodReplacer(
                AccessTools.Method(typeof(Pawn), nameof(Pawn.WorkTagIsDisabled)),
                AccessTools.Method(typeof(Harmony_NonViolentNonLethal_Patch), nameof(WorkTagIsDisabledExceptNonLethal))
            );
        }

        private static bool WorkTagIsDisabledExceptNonLethal(Pawn pawn, WorkTags workTags)
        {
            if (workTags == WorkTags.Violent && (pawn.equipment?.Primary?.def?.IsNonLethalWeapon() ?? false))
            {
                return false;
            }
            return pawn.WorkTagIsDisabled(workTags);
        }
    }

    //Make non-lethal weapons useable by non-violent pawns (melee)
    [HarmonyPatch]
    class Harmony_NonViolentNonLethalMelee_Patch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(FloatMenuUtility), nameof(FloatMenuUtility.GetMeleeAttackAction));
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.MethodReplacer(
                AccessTools.Method(typeof(Pawn), nameof(Pawn.WorkTagIsDisabled)),
                AccessTools.Method(typeof(Harmony_NonViolentNonLethalMelee_Patch), nameof(WorkTagIsDisabledExceptNonLethalMelee))
            );
        }

        private static bool WorkTagIsDisabledExceptNonLethalMelee(Pawn pawn, WorkTags workTags)
        {
            if (workTags == WorkTags.Violent && (pawn.equipment?.Primary?.def?.IsNonLethalMeleeWeapon() ?? false))
            {
                return false;
            }
            return pawn.WorkTagIsDisabled(workTags);
        }
    }

    //Make non-lethal weapons useable by non-violent pawns (ranged)
    [HarmonyPatch]
    class Harmony_NonViolentNonLethalRanged_Patch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(FloatMenuUtility), nameof(FloatMenuUtility.GetRangedAttackAction));
            yield return AccessTools.Method(typeof(VerbTracker), "CreateVerbTargetCommand");
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.MethodReplacer(
                AccessTools.Method(typeof(Pawn), nameof(Pawn.WorkTagIsDisabled)),
                AccessTools.Method(typeof(Harmony_NonViolentNonLethalRanged_Patch), nameof(WorkTagIsDisabledExceptNonLethalRanged))
            );
        }

        private static bool WorkTagIsDisabledExceptNonLethalRanged(Pawn pawn, WorkTags workTags)
        {
            if (workTags == WorkTags.Violent && (pawn.equipment?.Primary?.def?.IsNonLethalRangedWeapon() ?? false))
            {
                return false;
            }
            return pawn.WorkTagIsDisabled(workTags);
        }
    }
}
