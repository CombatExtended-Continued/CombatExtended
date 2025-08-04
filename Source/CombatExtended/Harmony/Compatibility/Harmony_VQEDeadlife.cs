using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended.HarmonyCE.Compatibility
{
    public static class Harmony_VQEDeadlife
    {
        [HarmonyPatch]
        public static class Harmony_VQEDeadlife_Apply
        {
            static MethodInfo TargetMethod()
            {
                // Dynamically resolve the method at runtime without loading the assembly directly
                return AccessTools.Method("VanillaQuestsExpandedDeadlife.Patches.CompWeaponDeteriorable:Notify_ShotFired");
            }

            public static bool Prepare()
            {
                return TargetMethod() != null;
            }

            // Prefix to check if the verb is Verb_LaunchProjectileCE
            // Returning true will execute the original method. Returning false will skip it.
            static bool Prefix(Verb verb, ref int ___shotsFired)
            {
                if (verb is Verb_LaunchProjectileCE)
                {
                    // Only increment shotsFired if the verb is Verb_LaunchProjectileCE
                    ___shotsFired++;
                    // Returning false will skip the rest of the original method
                    return true;
                }
                return true; // Execute the original method if not Verb_LaunchProjectileCE
            }

        }

    }
}
