using RimWorld;
using System.Collections.Generic;
using VanillaGravshipExpanded;
using Verse;
using Verse.Sound;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/TurretLinkerUtility.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended-Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion


namespace CombatExtended.Compatibility.VGECompat;

[StaticConstructorOnStartup]
public static class TurretLinkerCEUtility
{
    public static void StartLinking(this ITurretLinkerCE linker, float range)
    {
        var targetingParams = new TargetingParameters
        {
            canTargetPawns = false,
            canTargetBuildings = true,
            mapObjectTargetsMustBeAutoAttackable = false,
            validator = t => t.Thing != null
        };
        Find.Targeter.BeginTargeting(targetingParams, t =>
        {
            if (t.Thing is Building_GravshipTurretCE turret)
            {
                if (turret.Position.InHorDistOf(linker.LinkerPosition, range))
                {
                    linker.LinkTo(turret);
                }
                else
                {
                    Messages.Message("VGE_TargetOutOfRange".Translate(), MessageTypeDefOf.RejectInput, false);
                    SoundDefOf.ClickReject.PlayOneShotOnCamera();
                }
            }
            else
            {
                Messages.Message(linker.OnlyArtilleryErrorKey.Translate(), MessageTypeDefOf.RejectInput, false);
                SoundDefOf.ClickReject.PlayOneShotOnCamera();
            }
        }, onGuiAction: _ => GenDraw.DrawRadiusRing(linker.LinkerPosition, range));
    }

    public static IEnumerable<Gizmo> GetLinkerGizmos(this ITurretLinkerCE linker, float range)
    {
        int count = 0;
        foreach (var turret in linker.LinkedTurretsCE)
        {
            count++;
            var currentTurret = turret;
            yield return new Command_Action
            {
                defaultLabel = "VGE_UnlinkWithTurret".Translate(currentTurret.Label),
                defaultDesc = linker.UnlinkGizmoDesc,
                icon = TurretLinkerUtility.UnlinkIcon,
                action = currentTurret.Unlink
            };
            yield return new Command_Action
            {
                defaultLabel = "VGE_SelectLinkedTurret".Translate(currentTurret.Label),
                defaultDesc = linker.SelectGizmoDesc,
                icon = TurretLinkerUtility.SelectIcon,
                action = delegate
                {
                    Find.Selector.ClearSelection();
                    Find.Selector.Select(currentTurret);
                }
            };
        }

        int availableLinks = linker.MaxLinkedTurrets - count;
        for (int i = 0; i < availableLinks; i++)
        {
            yield return new Command_Action
            {
                defaultLabel = "VGE_LinkWithTurret".Translate(),
                defaultDesc = linker.LinkGizmoDesc,
                icon = TurretLinkerUtility.LinkIcon,
                action = () => linker.StartLinking(range)
            };
        }
    }
}

