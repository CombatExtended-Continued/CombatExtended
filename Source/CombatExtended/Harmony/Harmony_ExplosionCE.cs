using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Harmony;
using Verse;
using Random = UnityEngine.Random;

namespace CombatExtended.Harmony
{
    [HarmonyPatch(typeof(ExplosionCE), "GetDamageAmountAt")]
    public static class Harmony_ExplosionCE_GetDamageAmountAt
    {
        internal static bool Prefix(ExplosionCE __instance, ref float __result, IntVec3 c)
        {
            __result = __instance.GetDamageAmountAtCE(c);
            return false;
        }
    }

    [HarmonyPatch(typeof(ExplosionCE), "GetArmorPenetrationAt")]
    public class Harmony_ExplosionCE_GetArmorPenetrationAt
    {
        internal static bool Prefix(ExplosionCE __instance, ref float __result, IntVec3 c)
        {
            __result = __instance.GetArmorPenetrationAtCE(c);
            return false;
        }
    }
}