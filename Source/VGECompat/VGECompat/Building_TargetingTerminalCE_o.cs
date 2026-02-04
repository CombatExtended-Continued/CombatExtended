using RimWorld;
using System.Collections.Generic;
using VanillaGravshipExpanded;
using Verse;

namespace CombatExtended.Compatibility.VGECompat;

#region License
// Any VGE Code used for compatibility has been taken from the following source
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/Things/Building_TargetingTerminal.cs
#endregion

[StaticConstructorOnStartup]
public class Building_TargetingTerminalCE_o : Building_TargetingTerminal
{
    public Building_GravshipTurretCE_o linkedTurretCE;

    public override void ExposeData()
    {
        // skip the linkedTurret save/load, we don't need it
        linkedTurret = null;
        base.ExposeData();

        Scribe_References.Look(ref linkedTurretCE, "linkedTurretCE");
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        if (linkedTurretCE == null)
        {
            EnableOverlay();
        } 
        else
        {
            // Remove the overlay drawn by the base class
            DisableOverlay();
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

    public override IEnumerable<Gizmo> GetGizmos()
    {
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
        Find.Targeter.targetParams.validator = (TargetInfo t) => t.Thing is Building_GravshipTurretCE_o && t.Thing.Position.InHorDistOf(this.Position, 36);
        Find.Targeter.action = delegate (LocalTargetInfo t)
        {
            var turret = t.Thing as Building_GravshipTurretCE_o;
            LinkTo(turret);
        };
    }

    public void LinkTo(Building_GravshipTurretCE_o turret)
    {
        LinkTo(turret.ToBuilding_GravshipTurret);
        if (turret.linkedTerminal != null)
        {
            turret.linkedTerminal?.Unlink();
        }
        linkedTurretCE = turret;
        turret.LinkTo(this);
    }

    public new void Unlink()
    {
        base.Unlink();
        linkedTurretCE?.Unlink();
        linkedTurretCE = null;
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

