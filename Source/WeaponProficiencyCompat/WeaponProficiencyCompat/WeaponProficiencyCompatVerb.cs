using HarmonyLib;
using Verse;
using WeaponProficiency.Patches;

namespace CombatExtended.Compatibility.WeaponProficiencyVerb
{
    [HarmonyPatch(typeof(Pawn_HealthTracker_Notify_UsedVerb_WeaponProficiencyPatch))]
    [HarmonyPatch("IsValidVerb")]
    public static class IsValidVerb_PostfixPatch
    {
        // Correct postfix signature: use __result to modify the return value
        static void Postfix(Verb verb, ref bool __result)
        {
            if (verb is Verb_LaunchProjectileCE)
            {
                __result = true;
            }
        }
    }
}
