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
public class Building_TargetingTerminalCE : Building_TargetingTerminal
{
    public Building_GravshipTurretCE linkedTurretCE;
    
    public override void ExposeData()
    {
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
        // Run ThingWitComps.Tick without calling Building_TargetingTerminal.Tick again to avoid double-processing its logic
        if (comps != null)
        {
            int i = 0;
            for (int count = comps.Count; i < count; i++)
            {
                comps[i].CompTick();
            }
        }

        if (linkedTurretCE != null && (linkedTurretCE.Destroyed || !linkedTurretCE.Spawned))
        {
            UnlinkCE();
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
                    command.action = StartLinkingCE;
                    LinkWithTurretGizmo = command;
                    continue;
                }

                if (command.defaultLabel == "VGE_UnlinkWithTurret".Translate())
                {
                    command.action = UnlinkCE;
                    UnlinkWithTurretGizmo = command;
                    continue;
                }

                if (command.defaultLabel == "VGE_SelectLinkedTurret".Translate())
                {
                    command.action = SelectLinkedTurretCE;
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
    private void StartLinkingCE()
    {
        StartLinking();
        // intercept the targeting logic
        Find.Targeter.targetParams.validator = (TargetInfo t) => t.Thing is Building_GravshipTurretCE && t.Thing.Position.InHorDistOf(this.Position, 36);
        Find.Targeter.action = delegate (LocalTargetInfo t)
        {
            var turret = t.Thing as Building_GravshipTurretCE;
            LinkToCE(turret);
        };
    }

    public void LinkToCE(Building_GravshipTurretCE turret)
    {
        LinkTo(turret.ToBuilding_GravshipTurret);
        if (turret.linkedTerminal != null)
        {
            turret.linkedTerminal?.Unlink();
        }
        linkedTurretCE = turret;
        turret.LinkTo(this);
    }

    public void UnlinkCE()
    {
        Unlink();
        linkedTurretCE?.Unlink();
        linkedTurretCE = null;
    }

    public void SelectLinkedTurretCE()
    {
        if (linkedTurretCE != null)
        {
            Find.Selector.ClearSelection();
            Find.Selector.Select(linkedTurretCE);
        }
    }
}

