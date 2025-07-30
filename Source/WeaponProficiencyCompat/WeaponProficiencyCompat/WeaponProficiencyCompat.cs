using CombatExtended.Loader;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using WeaponProficiency.Patches;
using System;
using System.Diagnostics;
using System.Linq;
using RimWorld;
using UnityEngine;

namespace CombatExtended.Compatibility.WeaponProficiencyCompat
{
    
    public class WeaponProficiencyCompat : IModPart
    {
        private static Harmony harmony;

        public Type GetSettingsType()
        {
            return null;
        }

        public IEnumerable<string> GetCompatList()
        {
            yield break;
        }

        public void PostLoad(ModContentPack content, ISettingsCE _)
        {
            harmony = new Harmony("CombatExtended.Compatibility.WeaponProficiencyCompat");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker_Notify_UsedVerb_WeaponProficiencyPatch))]
    [HarmonyPatch("IsValidVerb")]
    public static class IsValidVerb_PostfixPatchCE
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
