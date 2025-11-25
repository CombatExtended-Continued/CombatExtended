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
    [HarmonyPatch]
    public static class Harmony_WeaponProficiency_Apply
    {
        static MethodInfo TargetMethod()
        {
            // Dynamically resolve the method at runtime without loading the assembly directly
            return AccessTools.Method("WeaponProficiency.Patches.Pawn_HealthTracker_Notify_UsedVerb_WeaponProficiencyPatch:IsValidVerb");
        }

        public static bool Prepare()
        {
            return TargetMethod() != null;
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
