using RimWorld;
using System.Collections.Generic;
using VanillaGravshipExpanded;
using Verse;
using Verse.Sound;

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
        // base.Tick(); // use harmony to call the grand parent Tick method ?

        if (linkedTurretCE != null && (linkedTurretCE.Destroyed || !linkedTurretCE.Spawned))
        {
            UnlinkCE();
        }
    }

    public override void DrawExtraSelectionOverlays()
    {
        base.DrawExtraSelectionOverlays();
        if (linkedTurretCE != null)
        {
            GenDraw.DrawLineBetween(this.TrueCenter(), linkedTurretCE.TrueCenter(), SimpleColor.White);
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
        {
            if (gizmo is Command_Action command &&
                (command.defaultLabel == "VGE_LinkWithTurret".Translate()
                || command.defaultLabel == "VGE_UnlinkWithTurret".Translate()
                || command.defaultLabel == "VGE_SelectLinkedTurret".Translate()
                ))
            {
                // skip base class gizmos
            } else {
                yield return gizmo;
            }
        }

        if (linkedTurretCE == null)
        {
            yield return new Command_Action
            {
                defaultLabel = "VGE_LinkWithTurret".Translate(),
                defaultDesc = "VGE_LinkWithTurretDesc".Translate(),
                icon = LinkIcon,
                action = delegate { StartLinkingCE(); }
            };
        }
        else
        {
            yield return new Command_Action
            {
                defaultLabel = "VGE_UnlinkWithTurret".Translate(),
                defaultDesc = "VGE_UnlinkWithTurretDesc".Translate(),
                icon = UnlinkIcon,
                action = delegate { UnlinkCE(); }
            };
            yield return new Command_Action
            {
                defaultLabel = "VGE_SelectLinkedTurret".Translate(),
                defaultDesc = "VGE_SelectLinkedTurretDesc".Translate(),
                icon = SelectIcon,
                action = delegate { SelectLinkedTurretCE(); }
            };
        }
    }
    private void StartLinkingCE()
    {
        var targetingParameters = new TargetingParameters
        {
            canTargetPawns = false,
            canTargetBuildings = true,
            mapObjectTargetsMustBeAutoAttackable = false,
            validator = (TargetInfo t) => t.Thing is Building_GravshipTurretCE && t.Thing.Position.InHorDistOf(this.Position, 36)
        };
        Find.Targeter.BeginTargeting(targetingParameters, delegate (LocalTargetInfo t)
        {
            var turret = t.Thing as Building_GravshipTurretCE;
            LinkToCE(turret);
        }, onGuiAction: delegate { GenDraw.DrawRadiusRing(this.Position, 36f); });
    }

    public void LinkToCE(Building_GravshipTurretCE turret)
    {
        if (turret.linkedTerminal != null)
        {
            turret.linkedTerminal?.Unlink();
        }
        linkedTurretCE = turret;
        turret.LinkTo(this);
        SoundDefOf.Tick_High.PlayOneShotOnCamera();
        DisableOverlay();
    }

    public void UnlinkCE()
    {
        linkedTurretCE?.Unlink();
        linkedTurretCE = null;
        SoundDefOf.Tick_Low.PlayOneShotOnCamera();
        EnableOverlay();
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

