using CombatExtended.Compatibility.VGECompat;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using VanillaGravshipExpanded2;
using Verse;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/VGE2LaunchInfo.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion


namespace CombatExtended.Compatibility.VGE2Compat;

[HarmonyPatch(typeof(VGE2LaunchInfo), nameof(VGE2LaunchInfo.ApplySignalJammerEffect))]
public class VGE2LaunchInfo_ApplySignalJammerEffect_Patch
{
    public static bool Prefix(Gravship gravship, VGE2LaunchInfo __instance)
    {
        var jammer = gravship.Engine.GravshipComponents
                  .Where(c => c.parent.def == InternalDefOf.SignalJammer)
                  .Select(c => c.parent.GetComp<CompSignalJammer>())
                  .FirstOrDefault(x => x != null && !x.OnCooldown);
        if (jammer is null) { 
            return false;
        }

        var map = gravship.Engine.Map;
        var enemyArtillery = new List<Building_GravshipTurretCE>();
        foreach (var b in map.listerBuildings.allBuildingsNonColonist)
        {
            if (b is Building_GravshipTurretCE turret && turret.HostileTo(Faction.OfPlayer))
            {
                enemyArtillery.Add(turret);
            }
        }

        if (enemyArtillery.Any())
        {
            jammer.StartCooldown();
            foreach (var art in enemyArtillery)
            {
                art.GetComp<CompStunnable>()?.StunHandler?.StunFor(30000, jammer.parent, true, true);
                FleckMaker.ThrowMicroSparks(art.DrawPos, map);
                for (int i = 0; i < 3; i++)
                {
                    FleckMaker.Static(art.OccupiedRect().RandomCell.ToVector3Shifted(), map, InternalDefOf.BlastEMP, 1f);
                }
            }

            Messages.Message("VGE_SignalJammerStunnedArtillery".Translate(), MessageTypeDefOf.PositiveEvent, false);
        }
        return false;
    }
}
