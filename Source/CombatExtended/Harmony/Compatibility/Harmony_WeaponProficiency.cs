using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended.HarmonyCE.Compatibility;

public static class Harmony_WeaponProficiency
{
    private static MethodInfo Verb_WeaponProficiency_Patch_HarmonyPatches
    {
        get
        {
            return AccessTools.Method("WeaponProficiency.Patches.Pawn_HealthTracker_Notify_UsedVerb_WeaponProficiencyPatch:IsValidVerb");
        }
    }
    [HarmonyPatch]
    public static class Harmony_WeaponProficiency_Apply
    {
        public static bool Prepare()
        {
            return Verb_WeaponProficiency_Patch_HarmonyPatches != null;
        }
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
