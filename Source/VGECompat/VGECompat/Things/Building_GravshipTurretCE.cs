using CombatExtended;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using VanillaGravshipExpanded;
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

public class Building_GravshipTurretCE: Building_TurretGunCEWithVGEAdapter
{

    public override Building_GravshipTurret GetBuilding_GravshipTurret(Building_TurretGunCEWithVGEAdapter instance)
    {
        return AdapterUtils<Building_GravshipTurretCE, Building_GravshipTurret>.DelegateValuesToTargetType((Building_GravshipTurretCE) instance);
    }

    // serialization fields
    // must duplicate composite fields here for saving/loading
    private float curAngle;
    private float rotationVelocity;
    private int barrelIndex = -1;


    public Building_TargetingTerminalCE linkedTerminal;
  
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

    public static Vector3 GetCastSource(Thing thing) => thing is Building_GravshipTurretCE turret ? turret.CastSource : thing.DrawPos;

    public void TrySwitchBarrel()
    {
        ToBuilding_GravshipTurret?.TrySwitchBarrel();
        barrelIndex = ToBuilding_GravshipTurret.barrelIndex;
    }

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

        var ext = def.GetModExtension<TurretExtension_RotationSpeed>();
        if (ext != null)
        {
            ToBuilding_GravshipTurret.rotationSpeed = ext.rotationSpeed;
        }

        var barrelExt = def.GetModExtension<TurretExtension_Barrels>();
        if (barrelExt != null)
        {
            ToBuilding_GravshipTurret.barrels = barrelExt.barrels;
        }

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

    public override void Tick()
    {
        base.Tick();
        ToBuilding_GravshipTurret.Tick();

        linkedTerminal = (Building_TargetingTerminalCE)ToBuilding_GravshipTurret.linkedTerminal;
        rotationVelocity = ToBuilding_GravshipTurret.rotationVelocity;
        curAngle = ToBuilding_GravshipTurret.curAngle;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref rotationVelocity, "rotationVelocity");
        Scribe_Values.Look(ref barrelIndex, "barrelIndex", -1);
        Scribe_References.Look(ref linkedTerminal, "linkedTerminal");
        Scribe_Values.Look(ref curAngle, "curAngle");
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

    public void LinkTo(Building_TargetingTerminalCE terminal)
    {
        ToBuilding_GravshipTurret.LinkTo(terminal);
        linkedTerminal = (Building_TargetingTerminalCE)ToBuilding_GravshipTurret.linkedTerminal;
        terminal.linkedTurretCE = this;
    }

    public void Unlink()
    {
        ToBuilding_GravshipTurret?.Unlink();
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
            var terminal = t.Thing as Building_TargetingTerminalCE;
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
                command3.icon = CompWorldArtillery.WorldTargetIcon;
                if (!CanFire)
                {
                    // skip this gizmo if we cannot fire
                    continue;
                }
            }

            yield return gizmo;
    
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
}

