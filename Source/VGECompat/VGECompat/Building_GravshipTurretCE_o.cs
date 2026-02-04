using CombatExtended;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using VanillaGravshipExpanded;
using VEF.Graphics;
using Verse;

namespace CombatExtended.Compatibility.VGECompat;

#region License
// Any VGE Code used for compatibility has been taken from the following source
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/Things/Building_GravshipTurret.cs
#endregion


/*
 * I tried to do a composition approach here. So `GravshipTurretWrapperCE composite` is the real business instance, Building_GravshipTurretCE
 * only retrieves data from it and passes calls to it.
 * If a technical limitation prevents this logic, I fall back to duplication.
 */
[StaticConstructorOnStartup]
public class Building_GravshipTurretCE_o: Building_TurretGunCEWithVGEAdapter
{

    public override Building_GravshipTurret GetBuilding_GravshipTurret(Building_TurretGunCEWithVGEAdapter instance)
    {
        return AdapterUtils<Building_GravshipTurretCE_o, Building_GravshipTurret>.DelegateValuesToTargetType((Building_GravshipTurretCE_o) instance);
    }

    public Building_TargetingTerminalCE_o linkedTerminal;

    public virtual bool CanFire => ToBuilding_GravshipTurret?.CanFire ?? false;

    public virtual bool CanAutoAttack => ToBuilding_GravshipTurret?.CanAutoAttack ?? false;
    public Pawn ManningPawn => ToBuilding_GravshipTurret?.ManningPawn;

    public virtual float GravshipTargeting => ToBuilding_GravshipTurret?.GravshipTargeting ?? 0f;

    protected virtual bool ShowNoLinkedTerminalOverlay => ToBuilding_GravshipTurret?.ShowNoLinkedTerminalOverlay ?? true;

    public Vector3 CastSource
    {
        get
        {
            if (ToBuilding_GravshipTurret != null)
            {
                return ToBuilding_GravshipTurret.CastSource;
            }
            return DrawPos;
        }
    }

    public static Vector3 GetCastSource(Thing thing) => thing is Building_GravshipTurretCE_o turret ? turret.CastSource : thing.DrawPos;

    protected override bool CanSetForcedTarget
    {
        get
        {
            if (ToBuilding_GravshipTurret.CanSetForcedTarget)
            {
                return true;
            }
            return false;
        }
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        ToBuilding_GravshipTurret.overlayDrawer = map.GetComponent<CustomOverlayDrawer>();
        if (linkedTerminal == null && ShowNoLinkedTerminalOverlay)
        {
            ToBuilding_GravshipTurret.EnableOverlay();
        }
        else
        {
            ToBuilding_GravshipTurret.DisableOverlay();    
        }
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        base.DeSpawn(mode);
        ToBuilding_GravshipTurret.overlayDrawer = null;
        ToBuilding_GravshipTurret = null;
    }

    protected void BaseTick()
    {
        base.Tick();
    }

    public override void Tick()
    {
        BaseTick();
        ToBuilding_GravshipTurret.Tick();

        linkedTerminal = (Building_TargetingTerminalCE_o)ToBuilding_GravshipTurret.linkedTerminal;

        if (ToBuilding_GravshipTurret.overlayDrawer.activeOverlays.ContainsKey(ToBuilding_GravshipTurret)) {
            var turretOverlay = ToBuilding_GravshipTurret.overlayDrawer.activeOverlays[ToBuilding_GravshipTurret];
            if (turretOverlay.overlays.Contains(VGEDefOf.VGE_NoLinkedTerminalOverlay))
            {
                ToBuilding_GravshipTurret.overlayDrawer?.Enable(this, VGEDefOf.VGE_NoLinkedTerminalOverlay);
            }
            else
            {
                ToBuilding_GravshipTurret.overlayDrawer?.Disable(this, VGEDefOf.VGE_NoLinkedTerminalOverlay);
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref linkedTerminal, "linkedTerminal");
    }

    public override string GetInspectString()
    {
        StringBuilder stringBuilder = new StringBuilder();
        string inspectString = base.GetInspectString();
        if (!inspectString.NullOrEmpty())
        {
            stringBuilder.AppendLine(inspectString);
        }

        // Logic from VGE
        if (Faction == Faction.OfPlayer && linkedTerminal == null)
        {
            stringBuilder.Append("VGE_NeedsLinkedTargetingTerminal".Translate());
        }
        return stringBuilder.ToString().TrimEndNewlines();
    }

    public void LinkTo(Building_TargetingTerminalCE_o terminal)
    {
        ToBuilding_GravshipTurret.LinkTo(terminal);
        linkedTerminal = (Building_TargetingTerminalCE_o)ToBuilding_GravshipTurret.linkedTerminal;
        terminal.linkedTurretCE = this;
    }

    public void Unlink()
    {
        ToBuilding_GravshipTurret?.Unlink();
        if (linkedTerminal != null)
        {
            linkedTerminal.linkedTurretCE = null;
        }
        linkedTerminal = null;
    }

    private void SelectLinkedTerminal()
    {
        ToBuilding_GravshipTurret.SelectLinkedTerminal();
    }
    private void StartLinking()
    {
        ToBuilding_GravshipTurret.StartLinking();
        // intercept the targeting logic
        Find.Targeter.targetParams.validator = (TargetInfo t) => t.Thing is Building_TargetingTerminal && t.Thing.Position.InHorDistOf(this.Position, 36);
        Find.Targeter.action = delegate (LocalTargetInfo t)
        {
            var terminal = t.Thing as Building_TargetingTerminalCE_o;
            LinkTo(terminal);
        };
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        Command_VerbTarget forceAttack = null;
        Command_Toggle holdFire = null;
        foreach (var gizmo in base.GetGizmos())
        {
            // skip these ones and save them because we will call them according to VGE logic

            if (gizmo is Command_VerbTarget command && command.defaultLabel == "CommandSetForceAttackTarget".Translate())
            {
                forceAttack = command;
                continue;
            }

            if (gizmo is Command_Toggle command2 && command2.defaultLabel == "CommandHoldFire".Translate())
            {
                holdFire = command2;
                continue;
            }

            if (gizmo is Command_ArtilleryTarget command3 && command3.defaultLabel == "CE_ArtilleryTargetLabel".Translate())
            {
                // skip this gizmo as we will add our own later
                continue;
            }

            yield return gizmo;

            // Add artillery command ourself
            if (CanFire)
            {
                Command_ArtilleryTarget wt = new Command_ArtilleryTarget()
                {
                    defaultLabel = "CE_ArtilleryTargetLabel".Translate(),
                    defaultDesc = "CE_ArtilleryTargetDesc".Translate(),
                    turret = this,
                    icon = CompWorldArtillery.WorldTargetIcon, // new icon
                    hotKey = KeyBindingDefOf.Misc5
                };
                yield return wt;
            }
        }

        foreach (var gizmo in ToBuilding_GravshipTurret.GetGizmos())
        {
            if (forceAttack != null && gizmo is Command_VerbTarget command1 && command1.defaultLabel == "CommandSetForceAttackTarget".Translate())
            {
                forceAttack.icon = command1.icon;
                forceAttack.disabled = command1.disabled;
                forceAttack.disabledReason = command1.disabledReason;
                yield return forceAttack;
                continue;
            }

            if (holdFire != null && gizmo is Command_Toggle command2 && command2.defaultLabel == "CommandHoldFire".Translate())
            {
                holdFire.icon = command2.icon;
                yield return holdFire;
                continue;
            }

            if (gizmo is Command_Action command3 && command3.defaultLabel == "VGE_LinkWithTerminal".Translate())
            {
                command3.action = StartLinking;
            }

            if (gizmo is Command_Action command4 && command4.defaultLabel == "VGE_UnlinkWithTerminal".Translate())
            {
                command4.action = Unlink;
            }

            if (gizmo is Command_Action command5 && command5.defaultLabel == "VGE_SelectLinkedTerminal".Translate())
            {
                command5.action = SelectLinkedTerminal;
            }

            yield return gizmo;
        }
    }

    // equivalent for Building_TurretGun_TryFindNewTarget_Patch in VGE
    public override LocalTargetInfo TryFindNewTarget()
    {
        if (CanAutoAttack is false)
        {
            return LocalTargetInfo.Invalid;
        }
        return base.TryFindNewTarget();
    }
}
