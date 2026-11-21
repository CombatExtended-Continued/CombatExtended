using CombatExtended.Compatibility.VGECompat;
using HarmonyLib;
using RimWorld;
using Verse;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/Harmony/Building_TurretGun_SpawnSetup_Patch.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

namespace CombatExtended.Compatibility.VGE2Compat;

// original patch applied on Building_TurretGun, but there is really no reason...
// I apply it only on patched Building_GravshipTurretCE turrets
[HarmonyPatch(typeof(Building_GravshipTurretCE), nameof(Building_GravshipTurretCE.SpawnSetup))]
public static class Building_GravshipTurretCE_SpawnSetup_Patch
{
    public static void Postfix(Building_GravshipTurretCE __instance)
    {
        LongEventHandler.ExecuteWhenFinished(delegate
        {
            if (__instance.Faction == Faction.OfPlayer)
            {
                __instance.ApplyForcedWarcomputerBuffs();
            }
        });
    }
}
