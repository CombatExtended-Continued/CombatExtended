#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

using CombatExtended.Compatibility.VGECompat;
using RimWorld;
using UnityEngine;
using VanillaGravshipExpanded2;
using Verse;

namespace CombatExtended.Compatibility.VGECompat2;

public class Building_EnemyAnticraftEmitterCE : Building_EnemyMechTurretCE
{
    private bool isFiringBurst = false;
    private Mote aimChargeMote;
    public AnticraftBeamStrike currentStrike;

    public override void PostSwapMap()
    {
        base.PostSwapMap();
        UpdatePowerOutput();
    }

    public override void PostMapInit()
    {
        base.PostMapInit();
        UpdatePowerOutput();
    }

    public override void Tick()
    {
        base.Tick();

        var shouldBeFiringBurst = CanFire && CurrentTarget.IsValid && Active && (AttackVerb.state == VerbState.Bursting || (currentStrike != null && !currentStrike.Destroyed));

        if (shouldBeFiringBurst != isFiringBurst)
        {
            isFiringBurst = shouldBeFiringBurst;
            UpdatePowerOutput();
        }

        if (isFiringBurst && powerComp is not { PowerOn: true })
        {
            currentTargetInt = LocalTargetInfo.Invalid;
            burstWarmupTicksLeft = 0;
            if (currentStrike != null && !currentStrike.Destroyed)
            {
                currentStrike.Destroy();
                currentStrike = null;
            }
            isFiringBurst = false;
            UpdatePowerOutput();
        }

        if (DeltaAngle <= 10 && CanFire && CurrentTarget.IsValid && Active && burstWarmupTicksLeft > 0)
        {
            if (aimChargeMote == null || aimChargeMote.Destroyed)
            {
                var verbProps = AttackVerb.verbProps;
                if (verbProps.aimingChargeMote != null)
                {
                    var scale = def.building.turretTopDrawSize / 4f;
                    aimChargeMote = MoteMaker.MakeStaticMote(Position.ToVector3Shifted(), Map, verbProps.aimingChargeMote, scale, makeOffscreen: true);
                }
            }
            if (aimChargeMote != null && !aimChargeMote.Destroyed)
            {
                var verbProps = AttackVerb.verbProps;
                var vector = CurrentTarget.CenterVector3 - Position.ToVector3Shifted();
                vector.y = 0f;
                vector.Normalize();
                var exactRotation = vector.AngleFlat();
                aimChargeMote.paused = IsStunned;
                aimChargeMote.exactRotation = exactRotation;
                aimChargeMote.exactPosition = Position.ToVector3Shifted() + vector * verbProps.aimingChargeMoteOffset;
                aimChargeMote.Maintain();
            }
        }
        else if (aimChargeMote != null && !aimChargeMote.Destroyed)
        {
            aimChargeMote.Destroy();
            aimChargeMote = null;
        }

        if (currentStrike != null && !currentStrike.Destroyed)
        {
            burstCooldownTicksLeft = Mathf.Max(burstCooldownTicksLeft, Mathf.RoundToInt(BurstCooldownTime() * 60f));
        }
    }

    private void UpdatePowerOutput()
    {
        if (powerComp != null)
        {
            var currentPowerConsumption = isFiringBurst ? powerComp.Props.basePowerConsumption : powerComp.Props.idlePowerDraw;
            powerComp.PowerOutput = 0f - currentPowerConsumption;
        }
    }

    public bool IsFiringBurst => isFiringBurst;

    public override void AbortFiringState()
    {
        base.AbortFiringState();
        if (currentStrike != null && !currentStrike.Destroyed)
        {
            currentStrike.Destroy();
        }
        currentStrike = null;
        isFiringBurst = false;
        UpdatePowerOutput();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref isFiringBurst, "isFiringBurst");
        Scribe_References.Look(ref currentStrike, "currentStrike");
    }
}
