using CombatExtended.Compatibility.VGECompat;
using HarmonyLib;
using System.Linq;
using VanillaGravshipExpanded2;
using Verse;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/GravshipThreatWorkers/GravshipThreatWorker_EnemyGravship.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion


namespace CombatExtended.Compatibility.VGE2Compat;

[HarmonyPatch(typeof(GravshipThreatWorker_EnemyGravship), nameof(GravshipThreatWorker_EnemyGravship.ShouldDefeat))]
public class GravshipThreatWorker_EnemyGravship_ShouldDefeat_Patch
{
    public static bool Prefix(Map map, GravshipThreatWorker_EnemyGravship __instance, ref bool __result)
    {
        if (map.listerThings.ThingsOfDef(InternalDefOf.VGE_LandingStructure_EnemyGravship).Any())
        {
            __result = false;
            return false;
        }
        var engineDestroyed = GravshipThreatWorker_OnDefeat_Patch.baseShouldDefeat(__instance, map);
        var artilleryDestroyed = !map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial).Any(x => x.Faction == __instance.EnemyFaction && x is Building_GravshipTurretCE);
        __result = engineDestroyed && artilleryDestroyed;
        return false;
    }
}
