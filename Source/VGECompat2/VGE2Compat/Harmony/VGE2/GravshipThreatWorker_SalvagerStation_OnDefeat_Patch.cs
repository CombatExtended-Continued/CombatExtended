using CombatExtended.Compatibility.VGECompat;
using HarmonyLib;
using RimWorld;
using System.Linq;
using VanillaGravshipExpanded2;
using Verse;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/GravshipThreatWorkers/GravshipThreatWorker_SalvagerStation.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

namespace CombatExtended.Compatibility.VGECompat2;

[HarmonyPatch(typeof(GravshipThreatWorker_SalvagerStation), nameof(GravshipThreatWorker_SalvagerStation.ShouldDefeat))]
public class GravshipThreatWorker_SalvagerStation_OnDefeat_Patch
{
    public static bool Prefix(Map map, GravshipThreatWorker_SalvagerStation __instance, ref bool __result)
    {
        var enemiesExist = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial)
            .Any(x => x.Faction == Faction.OfSalvagers && (x is Building_GravshipTurretCE || x.def == InternalDefOf.VGE_EnemyGravlockTether));
        __result = enemiesExist is false;
        return false;
    }
}
