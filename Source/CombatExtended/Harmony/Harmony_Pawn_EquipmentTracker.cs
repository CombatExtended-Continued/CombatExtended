using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.AI;

namespace CombatExtended.HarmonyCE
{
    /* Dev Notes:
     * A whole bunch of patches that all look like postfixes are the most apt.  Will have to look at each method in a case by case basis.
     */

    // Question from ProfoundDarkness: Are the "Cancel current job (use verb, etc.)" necessary?

    // They're needed to avoid bugs where pawns switch to another weapon but still use the old one 
    // -NIA


    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "Notify_PrimaryDestroyed")]
    static class Pawn_EquipmentTracker_AddEquipment
    {
        static void Postfix(Pawn_EquipmentTracker __instance, Pawn ___pawn)
        {
            //CE_Utility.TryUpdateInventory(pawn);   // Equipment was destroyed, update inventory

            // Try switching to the next available weapon
            ___pawn.TryGetComp<CompInventory>()?.SwitchToNextViableWeapon(false);
        }
    }

    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "TryDropEquipment")]
    static class Pawn_EquipmentTracker_TryDropEquipment
    {
        static void Postfix(Pawn_EquipmentTracker __instance, Pawn ___pawn)
        {
            // Cancel current job (use verb, etc.)
            if (___pawn.Spawned)
                ___pawn.stances.CancelBusyStanceSoft();
        }
    }

    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "TryTransferEquipmentToContainer")]
    static class Pawn_EquipmentTracker_TryTransferEquipmentToContainer
    {
        static void Postfix(Pawn_EquipmentTracker __instance, Pawn ___pawn)
        {
            // Cancel current job (use verb, etc.)
            if (___pawn.Spawned)
                ___pawn.stances.CancelBusyStanceSoft();
        }
    }

    /* Dev Notes: 
     * This one can't simply be done as a postfix.
     * The return does verb.TryStartCastOn() which changes game state.
     * On the other hand should be do-able with a method replacer...
     */

     /*
      * Commented out for A18 as the method seems to have been removed. Need to figure out where it went/whether this patch is still needed
      * -NIA
      */

    /*
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "TryStartAttack")]
    static class Pawn_EquipmentTracker_TryStartAttack
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            object localVerb = null;
            foreach (LocalBuilder local in Traverse.Create(il).Field("locals").GetValue<LocalBuilder[]>())
            {
                if (local.LocalType == typeof(Verb))
                {
                    localVerb = local;
                    break;
                }
            }

            foreach (CodeInstruction instruction in instructions)
            {
                // locate the callvirt verb...
                if (instruction.opcode == OpCodes.Callvirt && HarmonyBase.doCast((instruction.operand as MethodInfo)?.Name.Equals("TryStartCastOn")) 
                    && HarmonyBase.doCast((instruction.operand as MethodInfo).MemberType.Equals(typeof(Verb))))
                {
                    // insert the instance, arg0
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    // convert the current instruction to the new call.
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = AccessTools.Method(typeof(Pawn_EquipmentTracker_TryStartAttack), "ReloadCheck");
                }

                yield return instruction;
            }
        }

        static bool ReloadCheck(Verb verb, LocalTargetInfo targ, bool surpriseAttack, bool canFreeIntercept, Pawn_EquipmentTracker __instance)
        {
            // this is no longer valid: ThingWithComps primaryInt = Traverse.Create(__instance).Field("primaryInt").GetValue() as ThingWithComps;
            ThingOwner<ThingWithComps> equipment = Traverse.Create(__instance).Field("equipment").GetValue<ThingOwner<ThingWithComps>>();
            

            if (equipment != null && __instance.PrimaryEq != null && verb != null && verb == __instance.PrimaryEq.PrimaryVerb)
            {
                if (__instance.Primary != null)
                {
                    CompAmmoUser compAmmo = __instance.Primary.TryGetComp<CompAmmoUser>();
                    if (compAmmo != null && !compAmmo.CanBeFiredNow)
                    {
                        if (compAmmo.HasAmmo)
                        {
                            compAmmo.TryStartReload();
                        }
                        return false;
                    }
                }
            }
            return verb != null && verb.TryStartCastOn(targ, surpriseAttack, canFreeIntercept);
        }
    }
    */
}