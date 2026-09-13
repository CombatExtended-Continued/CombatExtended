using CombatExtended.Compatibility.VGECompat;
using HarmonyLib;
using System.Linq;
using System.Reflection;
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

[HarmonyPatch(typeof(GravshipThreatWorker_OrbitalCluster), nameof(GravshipThreatWorker_OrbitalCluster.ShouldDefeat))]
public class GravshipThreatWorker_OrbitalCluster_OnDefeat_Patch
{
    public static bool Prefix(Map map, GravshipThreatWorker_OrbitalCluster __instance, ref bool __result)
    {
        var engineExists = map.listerThings.ThingsOfDef(InternalDefOf.VGE_MechanoidGravTether).Any(x => x.Destroyed is false);
        var artilleryExists = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial)
            .Any(x => x.Faction == __instance.EnemyFaction && x is Building_GravshipTurretCE && x.Destroyed is false);

        __result = engineExists is false && artilleryExists is false;
        return false;
    }
}
