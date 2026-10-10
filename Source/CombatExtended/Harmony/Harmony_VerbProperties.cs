using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using HarmonyLib;

namespace CombatExtended.HarmonyCE;
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
                lock (cache)
                {
                    __result = typeof(Verb_LaunchProjectileCE).IsAssignableFrom(__instance.verbClass);
                    cache[__instance] = __result;
                }
            }
        }
    }
}

[HarmonyPatch(typeof(VerbProperties), nameof(VerbProperties.EffectiveMinRange), typeof(LocalTargetInfo), typeof(Thing))]
internal static class Harmony_VerbProperties_EffectiveMinRange
{
    // Covers callers of the vanilla method (OutOfRange, AttackTargetFinder validators, ...)
    internal static void Postfix(VerbProperties __instance, Thing caster, ref float __result)
    {
        if (!Verb_LaunchProjectileCE.AnyMinRangeOverride)
        {
            return;
        }
        // CurrentEffectiveVerb also resolves to the turret's verb when the caster is a pawn manning a turret
        if ((caster as IAttackTargetSearcher)?.CurrentEffectiveVerb is Verb_LaunchProjectileCE verb
            && verb.verbProps == __instance
            && verb.TryGetMinRangeOverride(out float value))
        {
            __result = value;
        }
    }
}

[HarmonyPatch(typeof(VerbProperties), nameof(VerbProperties.DrawRadiusRing))]
internal static class Harmony_VerbProperties_DrawRadiusRing
{
    // Vanilla draws the inner (minRange) ring from the raw field, so swap it for the duration of the draw.
    // Drawing runs on the main thread, so temporarily mutating the shared def is safe.
    internal static void Prefix(VerbProperties __instance, Verb verb, out float __state)
    {
        __state = __instance.minRange;
        if (Verb_LaunchProjectileCE.AnyMinRangeOverride && verb is Verb_LaunchProjectileCE ce && ce.TryGetMinRangeOverride(out float value))
        {
            __instance.minRange = value;
        }
    }

    // Finalizer, so the def's value is restored even if drawing throws
    internal static void Finalizer(VerbProperties __instance, float __state)
    {
        __instance.minRange = __state;
    }
}
