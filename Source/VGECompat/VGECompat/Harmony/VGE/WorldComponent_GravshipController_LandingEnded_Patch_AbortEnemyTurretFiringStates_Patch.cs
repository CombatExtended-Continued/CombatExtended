using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using VanillaGravshipExpanded;
using Verse;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/WorldComponent_GravshipController_LandingEnded_Patch.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion


namespace CombatExtended.Compatibility.VGECompat;

// -- Patchception --
// We need to patch the WorldComponent_GravshipController_LandingEnded_Patch.AbortEnemyTurretFiringStates method to ensure that CE hostile gravship
// turrets abort their firing state when the gravship lands, like it does in original mod's logic.

[HarmonyPatch(typeof(WorldComponent_GravshipController_LandingEnded_Patch), nameof(WorldComponent_GravshipController_LandingEnded_Patch.AbortEnemyTurretFiringStates))]
[HarmonyBefore("vanillaexpanded.gravship")] // Use priority to avoid crash
public class WorldComponent_GravshipController_LandingEnded_Patch_AbortEnemyTurretFiringStates_Patch
{
    public static void Postfix(Gravship gravship)
    {
        foreach (var map in Find.Maps)
        {
            foreach (var thing in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
            {
                if (thing is Building_GravshipTurretCE gravshipTurret && gravshipTurret.Faction != null && gravshipTurret.Faction.HostileTo(Faction.OfPlayer))
                {
                    // our equivalent to TryGetComp<CompWorldArtillery>().worldTarget is gravshipTurret.globalTargetInfo
                    if (!gravshipTurret.globalTargetInfo.IsValid || !gravship.Things.Contains(gravshipTurret.globalTargetInfo.Thing))
                    {
                        continue;
                    }
                    gravshipTurret.AbortFiringState();
                }
            }
        }
    }
}
