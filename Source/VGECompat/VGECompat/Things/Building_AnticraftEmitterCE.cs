using RimWorld;
using UnityEngine;
using Verse;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/Things/Building_AnticraftEmitter.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

namespace CombatExtended.Compatibility.VGECompat;

[StaticConstructorOnStartup]
public class Building_AnticraftEmitterCE: Building_GravshipTurretCE
{
    // serialization fields
    private bool isFiringBurst = false;
    private Mote aimChargeMote;

    // we don't have ammo set, so we get the defaultProjectile directly
    protected override ProjectilePropertiesCE ProjectileProps => (ProjectilePropertiesCE)GunCompEq.PrimaryVerb.verbProps.defaultProjectile.projectile;

    public override void PostSwapMap()
    {
        base.PostSwapMap();

        UpdatePowerOutput();
    }

    public override void PostMapInit()
    {
        base.PostSwapMap();

        UpdatePowerOutput();
    }

    public override void Tick()
    {
        base.Tick();

        bool shouldBeFiringBurst = CanFire && CurrentTarget.IsValid && Active && AttackVerb.state == VerbState.Bursting;

        if (shouldBeFiringBurst != isFiringBurst)
        {
            isFiringBurst = shouldBeFiringBurst;
            UpdatePowerOutput();
        }

        if (isFiringBurst && PowerComp is not CompPowerTrader { PowerOn: true })
        {
            ResetCurrentTarget();
            isFiringBurst = false;
            UpdatePowerOutput();
        }
        // use SignedAngle instead of their angleDiff
        if (DeltaAngle <= 10 && CanFire && CurrentTarget.IsValid && Active && burstWarmupTicksLeft > 0)
        {
            if (aimChargeMote == null || aimChargeMote.Destroyed)
            {
                var verbProps = AttackVerb.verbProps;
                if (verbProps.aimingChargeMote != null)
                {
                    aimChargeMote = MoteMaker.MakeStaticMote(Position.ToVector3Shifted(), Map, verbProps.aimingChargeMote, 1f, makeOffscreen: true);
                }
            }
            if (aimChargeMote != null && !aimChargeMote.Destroyed)
            {
                var verbProps = AttackVerb.verbProps;
                Vector3 vector = (CurrentTarget.CenterVector3 - Position.ToVector3Shifted());
                vector.y = 0f;
                vector.Normalize();
                float exactRotation = vector.AngleFlat();
                bool stunned = IsStunned;
                aimChargeMote.paused = stunned;
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
    }

    private void UpdatePowerOutput()
    {
        if (PowerComp is CompPowerTrader powerTrader)
        {
            var currentPowerConsumption = isFiringBurst ? PowerComp.Props.basePowerConsumption : PowerComp.Props.idlePowerDraw;
            powerTrader.PowerOutput = 0f - currentPowerConsumption;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref isFiringBurst, "isFiringBurst");
    }
}
