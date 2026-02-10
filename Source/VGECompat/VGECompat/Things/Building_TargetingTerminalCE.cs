using RimWorld;
using System.Collections.Generic;
using VanillaGravshipExpanded;
using Verse;
using Verse.Sound;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/Things/Building_TargetingTerminal.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

namespace CombatExtended.Compatibility.VGECompat;

[StaticConstructorOnStartup]
public class Building_TargetingTerminalCE : Building_TargetingTerminal
{
    public Building_GravshipTurretCE linkedTurretCE;

    public override void ExposeData()
    {
        // skip the linkedTurret save/load, we don't need it
        linkedTurret = null;
        base.ExposeData();

        Scribe_References.Look(ref linkedTurretCE, "linkedTurretCE");
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        linkedTurret = new Building_GravshipTurret(); // dummy to skip overlaty logic in base.SpawnSetup
        base.SpawnSetup(map, respawningAfterLoad);

        if (linkedTurretCE == null)
        {
            EnableOverlay();
        }
    }

    public override void Tick()
    {
        // skip the turret unlink from base.Tick (because this instance is not spawned and never will be)
        linkedTurret = null;
        base.Tick();

        // VGE logic
        if (linkedTurretCE != null && (linkedTurretCE.Destroyed || !linkedTurretCE.Spawned))
        {
            Unlink();
        }
    }

    public override void DrawExtraSelectionOverlays()
    {
        linkedTurret = null; // skip the turret unlink from base.DrawExtraSelectionOverlays
        base.DrawExtraSelectionOverlays();

        if (linkedTurretCE != null)
        {
            GenDraw.DrawLineBetween(this.TrueCenter(), linkedTurretCE.TrueCenter(), SimpleColor.White);
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        if (linkedTurretCE != null)
        {
            linkedTurret = new Building_GravshipTurret(); // dummy to skip base unlink logic
        }

        Gizmo LinkWithTurretGizmo = null;
        Gizmo UnlinkWithTurretGizmo = null;
        Gizmo SelectLinkedTurretGizmo = null;
        foreach (var gizmo in base.GetGizmos())
        {
            if (gizmo is Command_Action command)
            {
                if (command.defaultLabel == "VGE_LinkWithTurret".Translate())
                {
                    command.action = StartLinking;
                    LinkWithTurretGizmo = command;
                    continue;
                }

                if (command.defaultLabel == "VGE_UnlinkWithTurret".Translate())
                {
                    command.action = Unlink;
                    UnlinkWithTurretGizmo = command;
                    continue;
                }

                if (command.defaultLabel == "VGE_SelectLinkedTurret".Translate())
                {
                    command.action = SelectLinkedTurret;
                    SelectLinkedTurretGizmo = command;
                    continue;
                }
            } 

            yield return gizmo;
        }

        // copy VGE logic
        if (linkedTurretCE == null)
        {
            if (LinkWithTurretGizmo != null)
            {
                yield return LinkWithTurretGizmo;
            }
        }
        else if (UnlinkWithTurretGizmo != null && SelectLinkedTurretGizmo != null)
        {
            yield return UnlinkWithTurretGizmo;
            yield return SelectLinkedTurretGizmo;
        }
    }
    private new void StartLinking()
    {
        base.StartLinking();
        // intercept the targeting logic
        Find.Targeter.targetParams.validator = (TargetInfo t) => t.Thing is Building_GravshipTurretCE && t.Thing.Position.InHorDistOf(this.Position, 36);
        Find.Targeter.action = delegate (LocalTargetInfo t)
        {
            var turret = t.Thing as Building_GravshipTurretCE;
            LinkTo(turret);
        };
    }

    public void LinkTo(Building_GravshipTurretCE turret)
    {
        if (turret.linkedTerminal != null)
        {
            turret.linkedTerminal?.Unlink();
        }
        linkedTurretCE= turret;
        turret.LinkTo(this);
        SoundDefOf.Tick_High.PlayOneShotOnCamera();
        DisableOverlay();
    }

    public new void Unlink()
    {
        linkedTurretCE?.Unlink();
        linkedTurretCE = null;
        SoundDefOf.Tick_Low.PlayOneShotOnCamera();
        EnableOverlay();
    }

    public new void SelectLinkedTurret()
    {
        if (linkedTurretCE != null)
        {
            Find.Selector.ClearSelection();
            Find.Selector.Select(linkedTurretCE);
        }
    }
}

