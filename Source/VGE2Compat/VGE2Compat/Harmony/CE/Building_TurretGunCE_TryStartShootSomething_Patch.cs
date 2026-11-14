using HarmonyLib;
using VanillaGravshipExpanded2;
using Verse;
using Verse.Sound;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/Harmony/Building_TurretGun_TryStartShootSomething.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

namespace CombatExtended.Compatibility.VGE2Compat;

[HarmonyPatch(typeof(Building_TurretGunCE), nameof(Building_TurretGunCE.TryStartShootSomething))]
public static class Building_TurretGunCE_TryStartShootSomething_Patch
{
    public static void Postfix(Building_TurretGunCE __instance, LocalTargetInfo ___currentTargetInt)
    {
        if (__instance?.def == InternalDefOf.VGE_GiantWormspitter || __instance?.def == InternalDefOf.VFEI2_Siegeworm)
        {
            if (___currentTargetInt.IsValid)
            {
                InternalDefOf.VEG_InsectoidTurretTargetAcquired.PlayOneShot(new TargetInfo(__instance.Position, __instance.Map));
            }
        }
    }
}
