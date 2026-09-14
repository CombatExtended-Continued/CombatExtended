using CombatExtended.Compatibility.VGECompat;
using HarmonyLib;
using System.Linq;
using System.Reflection;
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


namespace CombatExtended.Compatibility.VGECompat2;

[HarmonyPatch(typeof(GravshipThreatWorker), nameof(GravshipThreatWorker.OnDefeat))]
public class GravshipThreatWorker_OnDefeat_Patch
{
    private static MethodInfo GravshipThreatWorker_ShouldDefeat = AccessTools.Method(typeof(GravshipThreatWorker), nameof(GravshipThreatWorker.ShouldDefeat));
    public static bool baseShouldDefeat(GravshipThreatWorker __instance, Map map)
    {
        return (bool)GravshipThreatWorker_ShouldDefeat.Invoke(__instance, [map]);
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
