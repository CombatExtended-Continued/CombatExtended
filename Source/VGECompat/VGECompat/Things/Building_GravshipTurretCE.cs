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

#region License
// Any VGE Code used for compatibility has been taken from the following source
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/Things/Building_GravshipTurret.cs
#endregion

/*
 * I duplicated the code from Building_GravshipTurret here, adapting it to work with CE.
 */
[StaticConstructorOnStartup]
public class Building_GravshipTurretCE: Building_TurretGunCE
{
    public Building_TargetingTerminalCE linkedTerminal;
    private CustomOverlayDrawer overlayDrawer;

    public virtual bool CanFire => linkedTerminal?.MannedByPlayer ?? false;

    public virtual bool CanAutoAttack => false;
    public Pawn ManningPawn => linkedTerminal?.MannableComp?.ManningPawn;

    // TODO: This field should be used to modify accuracy
    public virtual float GravshipTargeting => linkedTerminal?.GravshipTargeting ?? 0f;

    protected virtual bool ShowNoLinkedTerminalOverlay => true;

    protected override bool CanSetForcedTarget
    {
        get
        {
            if (linkedTerminal != null && linkedTerminal.MannedByPlayer)
            {
                return true;
            }
            return false;
        }
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        overlayDrawer = map.GetComponent<CustomOverlayDrawer>();
        if (linkedTerminal == null && ShowNoLinkedTerminalOverlay)
        {
            EnableOverlay();
        }
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        base.DeSpawn(mode);

        overlayDrawer = null;
    }

    public override void Tick()
    {
        base.Tick();
        if (linkedTerminal != null && (linkedTerminal.Destroyed || !linkedTerminal.Spawned))
        {
            Unlink();
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref linkedTerminal, "linkedTerminal");
    }

    public override string GetInspectString()
    {
        string text = base.GetInspectString();
        if (Faction == Faction.OfPlayer && linkedTerminal == null)
        {
            if (!text.NullOrEmpty())
            {
                text += "\n";
            }
            text += "VGE_NeedsLinkedTargetingTerminal".Translate();
        }
        return text;
    }

    public void LinkTo(Building_TargetingTerminalCE terminal)
    {
        terminal.linkedTurretCE?.Unlink();
        linkedTerminal = terminal;
        terminal.linkedTurretCE = this;
        SoundDefOf.Tick_High.PlayOneShotOnCamera();
        DisableOverlay();
        linkedTerminal.DisableOverlay();
    }

    public void Unlink()
    {
        // Add these two lines to stop targeting
        // Equivalent to Building_TurretGun_OrderAttack_Patch and Building_TurretGun_ResetForcedTarget_Patch (but better)
        ResetForcedTarget();
        ResetCurrentTarget();

        if (linkedTerminal != null)
        {
            linkedTerminal.EnableOverlay();
            linkedTerminal.linkedTurretCE = null;
        }
        linkedTerminal = null;
        SoundDefOf.Tick_Low.PlayOneShotOnCamera();
        if (ShowNoLinkedTerminalOverlay)
        {
            EnableOverlay();
        }
    }

    private void SelectLinkedTerminal()
    {
        if (linkedTerminal != null)
        {
            Find.Selector.ClearSelection();
            Find.Selector.Select(linkedTerminal);
        }
    }
    private void StartLinking()
    {
        var targetingParameters = new TargetingParameters
        {
            canTargetPawns = false,
            canTargetBuildings = true,
            mapObjectTargetsMustBeAutoAttackable = false,
            validator = (TargetInfo t) => t.Thing is Building_TargetingTerminalCE && t.Thing.Position.InHorDistOf(this.Position, 36)
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
            if (gizmo is Command_VerbTarget command && command.defaultLabel == "CommandSetForceAttackTarget".Translate())
            {
                command.icon = Building_GravshipTurret.ForceTargetIcon;
                if (!CanFire)
                {
                    command.Disable("VGE_NeedsMannedTargetingTerminal".Translate());
                }
            }
            else if (gizmo is Command_Toggle command2 && command2.defaultLabel == "CommandHoldFire".Translate())
            {
                command2.icon = Building_GravshipTurret.HoldFireIcon;
            }

            // Gravship Turret should always have a world Artillery command.
            // To avoid changing to much code in CE core, I skip it here, and add it again later
            // (my conflict was with the AnitcraftEmitter, which does not use AmmoComp, which denies the ArtilleryCommand button).
            if (gizmo is Command_ArtilleryTarget command3 && command3.defaultLabel == "CE_ArtilleryTargetLabel".Translate())
            {
                // skip this gizmo as we will add our own later
                continue;
            }

            yield return gizmo;
        }

        if (Faction != Faction.OfPlayer)
        {
            yield break;
        }

        // Add artillery command ourself
        if (CanFire)
        {
            Command_VGEArtilleryTarget wt = new Command_VGEArtilleryTarget()
            {
                defaultLabel = "CE_ArtilleryTargetLabel".Translate(),
                defaultDesc = "CE_ArtilleryTargetDesc".Translate(),
                turret = this,
                icon = CompWorldArtillery.WorldTargetIcon, // new icon
                hotKey = KeyBindingDefOf.Misc5,
                compWorldArtillery = this.TryGetComp<CompWorldArtillery>()
            };
            yield return wt;
        }

        if (linkedTerminal == null)
        {
            yield return new Command_Action
            {
                defaultLabel = "VGE_LinkWithTerminal".Translate(),
                defaultDesc = "VGE_LinkWithTerminalDesc".Translate(),
                icon = Building_GravshipTurret.LinkIcon,
                action = delegate { StartLinking(); }
            };
        }
        else
        {
            yield return new Command_Action
            {
                defaultLabel = "VGE_UnlinkWithTerminal".Translate(),
                defaultDesc = "VGE_UnlinkWithTerminalDesc".Translate(),
                icon = Building_GravshipTurret.UnlinkIcon,
                action = delegate { Unlink(); }
            };
            yield return new Command_Action
            {
                defaultLabel = "VGE_SelectLinkedTerminal".Translate(),
                defaultDesc = "VGE_SelectLinkedTerminalDesc".Translate(),
                icon = Building_GravshipTurret.SelectIcon,
                action = delegate { SelectLinkedTerminal(); }
            };
        }
    }

    public void EnableOverlay() => overlayDrawer?.Enable(this, VGEDefOf.VGE_NoLinkedTerminalOverlay);

    public void DisableOverlay() => overlayDrawer?.Disable(this, VGEDefOf.VGE_NoLinkedTerminalOverlay);

    #region adapting patch
    // HarmonyPatches/Building_TurretGun_Active_Patch
    public override bool Active {
        get {
            if (!CanFire)
            {
                return false;
            }
            return base.Active;
        }
    }

    // HarmonyPatches/Building_TurretGun_IsMortarOrProjectileFliesOverhead_Patch
    // No need to override IsMortarOrProjectileFliesOverhead, our code works

    // HarmonyPatches/Building_TurretGun_OrderAttack_Patch
    // No need to override OrderAttack, we don't use VGE CompWorldArtillery

    // HarmonyPatches/Building_TurretGun_ResetForcedTarget_Patch
    // No need to override ResetForcedTarget, we don't use VGE CompWorldArtillery

    // HarmonyPatches/Building_TurretGun_TryFindNewTarget_Patch
    public override LocalTargetInfo TryFindNewTarget()
    {
        if (!CanAutoAttack)
        {
            return LocalTargetInfo.Invalid;
        }
        return base.TryFindNewTarget();
    }

    // HarmonyPatches/Building_TurretGun_TryStartShootSomething_Patch
    // No need to override TryStartShootSomething, as we don't use their code for turret rotation

    #endregion
}

