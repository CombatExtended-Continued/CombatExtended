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

[HarmonyPatch(typeof(GravshipThreatWorker_EnemyGravjumper), nameof(GravshipThreatWorker_EnemyGravjumper.ShouldDefeat))]
public class GravshipThreatWorker_EnemyGravjumper_ShouldDefeat_Patch
{
    public static bool Prefix(Map map, GravshipThreatWorker_EnemyGravjumper __instance, ref bool __result)
    {
        if (map.listerThings.ThingsOfDef(InternalDefOf.VGE_LandingStructure_EnemyGravjumper).Any())
        {
            __result = false;
            return false;
        }

        bool engineDestroyed = GravshipThreatWorker_OnDefeat_Patch.baseShouldDefeat(__instance, map);
        var artilleryDestroyed = !map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial)
            .Any(x => x.Faction == __instance.EnemyFaction && x is Building_GravshipTurretCE);

        __result = engineDestroyed && artilleryDestroyed;
        return false;
    }
}
