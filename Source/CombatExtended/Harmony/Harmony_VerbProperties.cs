using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(VerbProperties))]
    [HarmonyPatch("LaunchesProjectile", MethodType.Getter)]
    internal static class Harmony_VerbProperties
    {
        private static Dictionary<Type, bool> cache = new Dictionary<Type, bool>();
        internal static void Postfix(VerbProperties __instance, ref bool __result)
        {
            if (!__result)
            {
		if (!cache.TryGetValue(__instance.GetType(), out __result)) {
		    __result = typeof(Verb_LaunchProjectileCE).IsAssignableFrom(__instance.verbClass);
		    cache[__instance.GetType()] = __result;
		}
            }
        }
    }
}
