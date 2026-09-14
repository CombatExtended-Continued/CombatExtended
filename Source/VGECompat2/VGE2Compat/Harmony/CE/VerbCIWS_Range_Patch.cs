using HarmonyLib;
using RimWorld;
using VanillaGravshipExpanded2;
using Verse;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/Harmony/CompPointDefence_InterceptionRadius_Patch.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

namespace CombatExtended.Compatibility.VGECompat2;

// Equivalent to CompPointDefence_InterceptionRadius_Patch
[HarmonyPatch(typeof(VerbCIWS), "Range", MethodType.Getter)]
public static class VerbCIWS_Range_Patch
{
    public static void Postfix(ThingComp __instance, ref float __result)
    {
        if (__instance.parent != null && __instance.parent.Faction == Faction.OfPlayer && __instance.parent.Map.IsWarcomputerPresent())
        {
            __result += __result * 0.33f; // I increase by 1/3 because that's what the VGE2 does. Even if they hardcoded 19.9f ...
        }
    }
}
