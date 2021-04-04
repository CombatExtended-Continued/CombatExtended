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
            bool patchApplied = false;
            List<CodeInstruction> instructionsArray = instructions.ToList();
            for (int i = 0; i < instructions.Count(); i++)
            {
                if ((instructionsArray[i].opcode == OpCodes.Call || instructionsArray[i].opcode == OpCodes.Callvirt) && ReferenceEquals(instructionsArray[i].operand, AccessTools.Method(typeof(Pawn), nameof(Pawn.WorkTagIsDisabled))))
                {
                    instructionsArray[i].opcode = OpCodes.Call;
                    instructionsArray[i].operand = AccessTools.Method(typeof(Harmony_AllowNonLethalWeaponsForNonViolentPawns_Patch), nameof(WorkTagIsDisabledExceptNonLethal));
                    patchApplied = true;
                    break;
                }
            }
            if (!patchApplied)
            {
                Log.Error($"{MethodBase.GetCurrentMethod().DeclaringType} did not find opcode to apply transpiler patch.");
            }
            return instructionsArray;
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
