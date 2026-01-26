using CombatExtended;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using VanillaGravshipExpanded;
using VEF.Graphics;
using Verse;
using Verse.Sound;

namespace CombatExtended.Compatibility.VGECompat;

public class Building_GravshipTurretCE: Building_TurretGunCE
{
    #region License
    // Any VGE Code used for compatibility has been taken from the following source
    // https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/Things/Building_GravshipTurret.cs
    #endregion

    private GravshipTurretWrapperCE composition;
   
    public Building_TargetingTerminalCE linkedTerminal;
  
    public virtual bool CanFire => composition?.CanFire ?? false;

    public virtual bool CanAutoAttack => composition?.CanAutoAttack ?? false;
    public Pawn ManningPawn => composition?.ManningPawn;

    public virtual float GravshipTargeting => composition?.GravshipTargeting ?? 0f;

    protected virtual bool ShowNoLinkedTerminalOverlay => composition?.ShowNoLinkedTerminalOverlay ?? true;

    public Vector3 CastSource
    {
        get
        {
            if (composition != null)
            {
                return composition.CastSource;
            }
            return DrawPos;
        }
    }

    public static Vector3 GetCastSource(Thing thing) => thing is Building_GravshipTurretCE turret ? turret.CastSource : thing.DrawPos;

    public void TrySwitchBarrel()
    {
        composition?.TrySwitchBarrel();
    }

    protected override bool CanSetForcedTarget
    {
        get
        {
            if (composition.CanSetForcedTarget)
            {
                return true;
            }
            return false;
        }
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        composition = new GravshipTurretWrapperCE(this);

        var ext = def.GetModExtension<TurretExtension_RotationSpeed>();
        if (ext != null)
        {
            composition.rotationSpeed = ext.rotationSpeed;
        }

        var barrelExt = def.GetModExtension<TurretExtension_Barrels>();
        if (barrelExt != null)
        {
            composition.barrels = barrelExt.barrels;
        }

        if (linkedTerminal == null && ShowNoLinkedTerminalOverlay)
        {
            composition.EnableOverlay();
        }
        else
        {
            composition.DisableOverlay();    
        }
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        base.DeSpawn(mode);
        composition.overlayDrawer = null;
    }

    public override void Tick()
    {
        base.Tick();
        composition.Tick();


        linkedTerminal = (Building_TargetingTerminalCE)composition.linkedTerminal;
        top.CurRotation = composition.top.CurRotation;
        burstWarmupTicksLeft = composition.burstWarmupTicksLeft;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref composition.rotationVelocity, "rotationVelocity");
        Scribe_Values.Look(ref composition.barrelIndex, "barrelIndex", -1);
        Scribe_References.Look(ref linkedTerminal, "linkedTerminal");
        Scribe_Values.Look(ref composition.curAngle, "curAngle");
    }

    public override string GetInspectString()
    {
        StringBuilder stringBuilder = new StringBuilder();
        string inspectString = base.GetInspectString();
        if (!inspectString.NullOrEmpty())
        {
            stringBuilder.AppendLine(inspectString);
        }

        if (Faction == Faction.OfPlayer && linkedTerminal == null)
        {
            stringBuilder.Append("VGE_NeedsLinkedTargetingTerminal".Translate());
        }
        return stringBuilder.ToString().TrimEndNewlines();
    }

    public void LinkTo(Building_TargetingTerminalCE terminal)
    {
        composition.LinkTo(terminal);
        linkedTerminal = (Building_TargetingTerminalCE)composition.linkedTerminal;
        terminal.linkedTurretCE = this;
    }

    public void Unlink()
    {
        composition.Unlink();
        linkedTerminal = null;
    }

    private void SelectLinkedTerminal()
    {
        composition.SelectLinkedTerminal();
    }
    private void StartLinking()
    {
        var targetingParameters = new TargetingParameters
        {
            canTargetPawns = false,
            canTargetBuildings = true,
            mapObjectTargetsMustBeAutoAttackable = false,
            validator = (TargetInfo t) => t.Thing is Building_TargetingTerminal && t.Thing.Position.InHorDistOf(this.Position, 36)
        };
        Find.Targeter.BeginTargeting(targetingParameters, delegate (LocalTargetInfo t)
        {
            var terminal = t.Thing as Building_TargetingTerminalCE;
            LinkTo(terminal);
        }, onGuiAction: delegate { GenDraw.DrawRadiusRing(this.Position, 36f); });
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
        {
            if (gizmo is Command_VerbTarget command && command.defaultLabel == "CommandSetForceAttackTarget".Translate()
                || gizmo is Command_Toggle command2 && command2.defaultLabel == "CommandHoldFire".Translate())
            {
                // skip these ones
            }
            else
            {
                yield return gizmo;
            }
        }

        foreach (var gizmo in composition.GetGizmos())
        {
            // rewrite gizmos
            if (gizmo is Command_Action command && command.defaultLabel == "VGE_LinkWithTerminal".Translate())
            {
                command.action = StartLinking;
            }

            if (gizmo is Command_Action command2 && command2.defaultLabel == "VGE_UnlinkWithTerminal".Translate())
            {
                command2.action = Unlink;
            }

            if (gizmo is Command_Action command3 && command3.defaultLabel == "VGE_SelectLinkedTerminal".Translate())
            {
                command3.action = SelectLinkedTerminal;
            }

            yield return gizmo;
        }
    }
}

