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
        private static Dictionary<VerbProperties, bool> cache = new Dictionary<VerbProperties, bool>();
        internal static void Postfix(VerbProperties __instance, ref bool __result)
        {
            if (!__result)
            {
                if (!cache.TryGetValue(__instance, out __result))
                {
                    __result = typeof(Verb_LaunchProjectileCE).IsAssignableFrom(__instance.verbClass);
                    cache[__instance] = __result;
                }
            }
        }
    }
}
