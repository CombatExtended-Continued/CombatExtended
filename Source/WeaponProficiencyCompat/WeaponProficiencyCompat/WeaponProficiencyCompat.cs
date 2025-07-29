using System;
using CombatExtended;
using HarmonyLib;
using Verse;

namespace CombatExtended.Compatibility.WeaponProficiency
{
    [HarmonyPatch(typeof(Pawn_HealthTracker_Notify_UsedVerb_WeaponProficiencyPatch), "IsValidVerb")]
    public static class IsValidVerb_PostfixPatch
    {
        private static bool Postfix(Verb verb, ref bool postfix_result)
        {
            if (verb is Verb_LaunchProjectileCE)
            {
                return true;
            }

            // Continue with the original logic or modify as needed.
            return postfix_result;
        }
    }
}
