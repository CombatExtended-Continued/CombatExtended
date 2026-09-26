using HarmonyLib;
using UnityEngine;
using VanillaGravshipExpanded;
using Verse;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/DynamicDrawManager_DrawDynamicThings_Patch.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

namespace CombatExtended.Compatibility.VGECompat;

[HarmonyPatch(typeof(DynamicDrawManager), nameof(DynamicDrawManager.DrawDynamicThings))]
public static class DynamicDrawManager_DrawDynamicThings_Patch
{
    public static void Postfix(DynamicDrawManager __instance)
    {
        foreach (var thing in __instance.DrawThings)
        {
            if (thing is Building_TurretGunCE gun && (gun is Building_GravshipTurretCE || Building_TurretGun_TryStartShootSomething_Patch.IsPointDefenseTurret(gun.def)))
            {
                if (gun.Map.fogGrid.IsFogged(gun.Position))
                {
                    gun.top.DrawTurret(gun.DrawPos, Vector3.zero, 0f);
                }
            }
        }
    }
}

