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
    //Make non-lethal weapons useable by non-violent pawns
    [HarmonyPatch]
    class Harmony_AllowNonLethalWeaponsForNonViolentPawns_Patch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Pawn), nameof(Pawn.TryStartAttack));
            yield return AccessTools.Method(typeof(VerbTracker), "CreateVerbTargetCommand");
            yield return AccessTools.Method(typeof(FloatMenuUtility), nameof(FloatMenuUtility.GetRangedAttackAction));
            yield return AccessTools.Method(typeof(FloatMenuUtility), nameof(FloatMenuUtility.GetMeleeAttackAction));
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.MethodReplacer(
                AccessTools.Method(typeof(Pawn), nameof(Pawn.WorkTagIsDisabled)),
                AccessTools.Method(typeof(Harmony_AllowNonLethalWeaponsForNonViolentPawns_Patch), nameof(WorkTagIsDisabledExceptNonLethal))
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
}
