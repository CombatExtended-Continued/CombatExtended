using HarmonyLib;
using RimWorld;
using VanillaGravshipExpanded2;
using Verse;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/WorldComponent_GravshipController_LandingEnded_Patch.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

namespace CombatExtended.Compatibility.VGECompat2;

[HarmonyPatch(typeof(Building_TurretGunCE), "Tick")]
public static class Building_TurretGunCE_Tick_Patch
{
    public static void Postfix(Building_TurretGunCE __instance)
    {
        if (__instance.IsHashIntervalTick(250) && __instance.Faction == Faction.OfPlayer)
        {
            __instance.ApplyForcedMissRadiusBuffs();
        }
    }
}
