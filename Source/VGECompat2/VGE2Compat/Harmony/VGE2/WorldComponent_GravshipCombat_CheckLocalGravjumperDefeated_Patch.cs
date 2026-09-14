using CombatExtended.Compatibility.VGECompat;
using HarmonyLib;
using RimWorld;
using System.Linq;
using VanillaGravshipExpanded2;
using Verse;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/WorldObjects/WorldComponent_GravshipCombat.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

namespace CombatExtended.Compatibility.VGECompat2;

[HarmonyPatch(typeof(WorldComponent_GravshipCombat), nameof(WorldComponent_GravshipCombat.CheckLocalGravjumperDefeated))]
public class WorldComponent_GravshipCombat_CheckLocalGravjumperDefeated_Patch
{
    public static bool Prefix(Map map, WorldComponent_GravshipCombat __instance)
    {
        if (!__instance.gravjumperLandedLocal)
        { 
            return false; 
        }

        if (map.listerThings.ThingsOfDef(InternalDefOf.VGE_LandingStructure_EnemyGravjumper).Any())
        {
            return false;
        }

        var engineExists = map.listerThings.ThingsOfDef(InternalDefOf.VGE_EnemyGravjumperEngine).Any(x => !x.Destroyed);
        var turretsExist = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial)
            .Any(x => x.Faction == Faction.OfSalvagers && x is Building_GravshipTurretCE && !x.Destroyed);

        if (!engineExists && !turretsExist)
        {
            __instance.gravjumperLandedLocal = false;
            Find.LetterStack.ReceiveLetter("VGE_GravjumperDefeated".Translate(), "VGE_GravjumperDefeatedDesc".Translate(), LetterDefOf.PositiveEvent);
        }
        return false;
    }
}
