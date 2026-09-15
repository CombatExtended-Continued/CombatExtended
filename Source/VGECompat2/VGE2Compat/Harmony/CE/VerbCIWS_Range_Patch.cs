using HarmonyLib;
using RimWorld;
using VanillaGravshipExpanded2;
using Verse;

namespace CombatExtended.Compatibility.VGECompat2;

// Equivalent to CompPointDefence_InterceptionRadius_Patch
// see https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/Harmony/CompPointDefence_InterceptionRadius_Patch.cs
[HarmonyPatch(typeof(Building_CIWS_CE), nameof(Building_Turret_MultiVerbs.AttackVerb), MethodType.Getter)]
public class VerbCIWS_Range_Patch
{
    public static void Postfix(Building_CIWS_CE __instance, ref Verb __result)
    {
        if (__instance.Faction == Faction.OfPlayer && __instance.Map.IsWarcomputerPresent())
        {
            __result.verbProps.range += __result.verbProps.range * 0.33f; // I increase by 1/3 because that's what the VGE2 does. Even if they hardcoded 19.9f ...
        }
    }
}
