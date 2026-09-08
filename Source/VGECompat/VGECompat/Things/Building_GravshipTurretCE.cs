using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using VanillaGravshipExpanded;
using VEF.Graphics;
using Verse;
using Verse.Sound;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/Things/Building_GravshipTurret.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

namespace CombatExtended.Compatibility.VGECompat;


// I duplicated only part of the code from Building_GravshipTurret here, adapting it to work with CE. Major differences with the original code are:
// - We use our own turret rotation code
// - We use our own world map artillery code
// - We use our own ammunition box system
// - Calculation of Forced Miss Radius is removed to be replaced by CE targeting system (ShiftVecReportFor)
[StaticConstructorOnStartup]
public class Building_GravshipTurretCE : Building_TurretGunCE
{
    public ITurretLinkerCE linkedTerminal;
    private CustomOverlayDrawer overlayDrawer;
    public bool unlinking;

    public bool permanentlyDisabled;
    public void DisablePermanently()
    {
        permanentlyDisabled = true;
        currentTargetInt = LocalTargetInfo.Invalid;
        forcedTarget = LocalTargetInfo.Invalid;
        burstWarmupTicksLeft = 0;
        if (Faction is not null)
        {
            SetFaction(null);
        }
    }

    public virtual void AbortFiringState()
    {
        // burstActivated is not present in Building_TurretGunCE, we ignore it
        // burstActivated = false;
        if (AttackVerb != null)
        {
            AttackVerb.state = VerbState.Idle;
            AttackVerb.burstShotsLeft = 0;
            AttackVerb.ticksToNextBurstShot = 0;
        }
        ResetForcedTarget();
    }

    public virtual bool CanFire => !permanentlyDisabled && (linkedTerminal?.MannedByPlayer ?? false);

    public virtual bool CanAutoAttack => false;
    public Pawn ManningPawn => linkedTerminal?.ManningPawn;

    // Used in Verb_ShootWithVGETargeting.ShiftVecReportFor to balance ShiftVecReport
    public virtual float GravshipTargeting => linkedTerminal?.GravshipTargeting ?? 0f;

    protected virtual bool ShowNoLinkedTerminalOverlay => true;

    protected override bool CanSetForcedTarget => !permanentlyDisabled && linkedTerminal != null && linkedTerminal.MannedByPlayer;

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
        if (linkedTerminal != null)
        {
            var linkerThing = linkedTerminal.LinkerThing;
            if (linkerThing is null || linkerThing.Destroyed || !linkedTerminal.LinkerThing.Spawned && linkedTerminal is not Apparel)
            {
                Unlink();
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref linkedTerminal, "linkedTerminal");
        Scribe_Values.Look(ref permanentlyDisabled, "permanentlyDisabled");
    }

    public override string GetInspectString()
    {
        string text = base.GetInspectString();
        if (permanentlyDisabled)
        {
            if (!text.NullOrEmpty())
            {
                text += "\n";
            }
            text += "VGE_PermanentlyDisabled".Translate();
        }
        else if (ShowNoLinkedTerminalOverlay && Faction == Faction.OfPlayer && linkedTerminal == null)
        {
            if (!text.NullOrEmpty())
            {
                text += "\n";
            }
            text += "VGE_NeedsLinkedTargetingTerminal".Translate();
        }
        return text;
    }

    public void LinkTo(ITurretLinkerCE terminal)
    {
        if (linkedTerminal == terminal)
        {
            return;
        }
        if (linkedTerminal != null && linkedTerminal != terminal)
        {
            linkedTerminal.Unlink(this);
        }
        linkedTerminal = terminal;

        if (terminal != null && !terminal.LinkedTurretsCE.Contains(this))
        {
            terminal.LinkTo(this);
        }

        SoundDefOf.Tick_High.PlayOneShotOnCamera();
        DisableOverlay();
        linkedTerminal.DisableOverlay();
    }

    public void Unlink()
    {
        // Add these two lines to stop targeting
        // Equivalent to Building_TurretGun_OrderAttack_Patch and Building_TurretGun_ResetForcedTarget_Patch
        ResetForcedTarget();
        ResetCurrentTarget();

        var prevTerminal = linkedTerminal;
        linkedTerminal = null;
        if (prevTerminal != null && !unlinking)
        {
            prevTerminal.Unlink(this);
        }
        else
        {
            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            if (ShowNoLinkedTerminalOverlay)
            {
                EnableOverlay();
            }
        }
    }

    private void SelectLinkedTerminal()
    {
        if (linkedTerminal != null && linkedTerminal.LinkerThing != null)
        {
            Find.Selector.ClearSelection();
            Find.Selector.Select(linkedTerminal.LinkerThing);
        }
        else if (linkedTerminal != null)
        {
            Find.Selector.ClearSelection();
            Find.Selector.Select((Thing)linkedTerminal);
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

    public override LocalTargetInfo TryFindNewTarget()
    {
        // Equivalent to Building_TurretGun_TryFindNewTarget_Patch
        if (!CanAutoAttack)
        {
            return LocalTargetInfo.Invalid;
        }

        if (permanentlyDisabled)
        {
            return LocalTargetInfo.Invalid;
        }
        return base.TryFindNewTarget();
    }

    public override void OrderAttack(LocalTargetInfo targ)
    {
        if (permanentlyDisabled)
        {
            return;
        }
        base.OrderAttack(targ);
    }

    public override void DrawExtraSelectionOverlays()
    {
        base.DrawExtraSelectionOverlays();
        if (linkedTerminal != null && linkedTerminal.LinkerThing != null && linkedTerminal.LinkerThing.Spawned)
        {
            GenDraw.DrawLineBetween(this.TrueCenter(), linkedTerminal.LinkerThing.DrawPos, SimpleColor.White);
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
        {
            if (gizmo is Command_VerbTarget command && command.defaultLabel == "CommandSetForceAttackTarget".Translate())
            {
                command.icon = Building_GravshipTurret.ForceTargetIcon;
                if (linkedTerminal is Apparel)
                {
                    command.Disable("VGE_MustBeAimedViaEquippedTargeter".Translate());
                }
                else if (!CanFire)
                {
                    command.Disable("VGE_NeedsMannedTargetingTerminal".Translate());
                }
            }
            else if (gizmo is Command_Toggle command2 && command2.defaultLabel == "CommandHoldFire".Translate())
            {
                command2.icon = Building_GravshipTurret.HoldFireIcon;
            }

            // In VGE, this gizmo is added by CompWorldArtillery.
            // CE should automatically add it, but we need a custom Gizmo for Gravship Turret we so skip it here, and add it again later
            if (gizmo is Command_ArtilleryTarget command4 && command4.defaultLabel == "CE_ArtilleryTargetLabel".Translate())
            {
                // skip this gizmo as we will add our own later
                continue;
            }

            yield return gizmo;
        }

        if (Faction != Faction.OfPlayer || permanentlyDisabled)
        {
            yield break;
        }

        // --- Manually add artillery command as said previously ---
        if (CanFire)
        {
            Command_VGEArtilleryTarget wt = new Command_VGEArtilleryTarget()
            {
                defaultLabel = "CE_ArtilleryTargetLabel".Translate(),
                defaultDesc = "CE_ArtilleryTargetDesc".Translate(),
                turret = this,
                icon = CompWorldArtillery.WorldTargetIcon, // icon from VGE
                hotKey = KeyBindingDefOf.Misc5,
                compWorldArtillery = this.TryGetComp<CompWorldArtilleryCE>(),
            };
            yield return wt;
        }
        // ----------------------------------------------------------

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
    public override bool Active
    {
        get
        {
            if (!CanFire)
            {
                return false;
            }
            return base.Active;
        }
    }
    #endregion
}

