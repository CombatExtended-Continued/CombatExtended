using HarmonyLib;
using Verse;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/ShotReport_HitFactorFromShooter_Patch.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

namespace CombatExtended.Compatibility.VGECompat;

[HarmonyPatch(typeof(ShotReport), "HitReportFor")]
public static class ShotReport_HitReportFor_Patch
{
    public static void Postfix(ref ShotReport __result, Thing caster, Verb verb, LocalTargetInfo target)
    {
        if (caster is Building_GravshipTurretCE turret)
        {
            __result.factorFromTargetSize = 1f;
        }
    }
}
