using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using CombatExtended;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(Explosion), "GetDamageAmountAt")]
    public static class Harmony_ExplosionCE_GetDamageAmountAt
    {
        internal static bool Prefix(Explosion __instance, ref int __result, IntVec3 c)
        {
            if (__instance is ExplosionCE explosionCE)
            {
                __result = explosionCE.GetDamageAmountAtCE(c);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Explosion), "GetArmorPenetrationAt")]
    public class Harmony_ExplosionCE_GetArmorPenetrationAt
    {
        internal static bool Prefix(Explosion __instance, ref float __result, IntVec3 c)
        {
            if (__instance is ExplosionCE explosionCE)
            {
                __result = explosionCE.GetArmorPenetrationAtCE(c);
                return false;
            }
            return true;
        }
    }
}