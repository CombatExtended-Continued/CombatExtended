using CombatExtended.Compatibility.VGECompat;
using HarmonyLib;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using VanillaGravshipExpanded2;
using Verse;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/GravshipThreatWorkers/GravshipThreatWorker.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion


namespace CombatExtended.Compatibility.VGE2Compat;

[HarmonyPatch(typeof(GravshipThreatWorker), nameof(GravshipThreatWorker.OnDefeat))]
public class GravshipThreatWorker_OnDefeat_Patch
{
    // Create a base call on base.GravshipThreatWorker()
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(GravshipThreatWorker), nameof(GravshipThreatWorker.ShouldDefeat))]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool baseShouldDefeat(GravshipThreatWorker __instance, Map map)
    {
        // This is just a stub, because Harmony will copy the original code of ShouldDefeat.
        throw new NotImplementedException();
    }

    public static bool Prefix(Map map)
    {
        foreach (var thing in map.listerBuildings.allBuildingsNonColonist.OfType<Building_GravshipTurretCE>()) 
        {
            thing.DisablePermanently();
        }
        return false;
    }
}
