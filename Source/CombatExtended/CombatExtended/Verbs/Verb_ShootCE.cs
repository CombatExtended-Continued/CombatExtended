using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace CombatExtended
{
    public class Verb_ShootCE : Verb_LaunchProjectileCE
    {
        #region Constants

        // How much time to spend on aiming
        private const int AimTicksMin = 30;
        private const int AimTicksMax = 240;

        // XP amounts as taken from vanilla Verb_Shoot
        private const float PawnXp = 20f;
        private const float HostileXp = 170f;

        // Suppression aim penalty
        private const float SuppressionSwayFactor = 1.5f;

        #endregion

        #region Fields

        private bool _isAiming;

        #endregion

        #region Properties

        public bool isBipodGun
        {
            get
            {
                return ((this.EquipmentSource?.TryGetComp<BipodComp>() ?? null) != null);

            }
        }
        public override int ShotsPerBurst
        {
            get
            {                
                return CompFireModes != null ? ShotsPerBurstFor(CompFireModes.CurrentFireMode) : VerbPropsCE.burstShotCount;
            }
        }

        private bool ShouldAim
        {
            get
            {
                if (CompFireModes != null)
                {
                    if (ShooterPawn != null)
                    {
                        // Check for hunting job
                        if (ShooterPawn.CurJob != null && ShooterPawn.CurJob.def == JobDefOf.Hunt)
                            return true;

                        // Check for suppression
                        if (IsSuppressed) return false;

                        // Check for RunAndGun mod
                        if (ShooterPawn.pather?.Moving ?? false)
                        {
                            return false;
                        }
                    }
                    return CompFireModes.CurrentAimMode == AimMode.AimedShot;
                }
                return false;
            }
        }

        public override float SwayAmplitude
        {
            get
            {
                var sway = base.SwayAmplitude;
                if (ShouldAim)
                    return sway * Mathf.Max(0, 1 - AimingAccuracy) / Mathf.Max(1, SightsEfficiency);
                else if (IsSuppressed)
                    return sway * SuppressionSwayFactor;
                return sway;
            }
        }

        public float SpreadDegrees
        {
            get
            {
                return (EquipmentSource?.GetStatValue(StatDef.Named("ShotSpread")) ?? 0) * (projectilePropsCE != null ? projectilePropsCE.spreadMult : 0f);
            }
        }

        // Whether our shooter is currently under suppressive fire
        private bool IsSuppressed => ShooterPawn?.TryGetComp<CompSuppressable>()?.isSuppressed ?? false;

        #endregion

        #region Methods

        public float SwayAmplitudeFor(AimMode mode)
        {
            float sway = base.SwayAmplitude;
            if (ShouldAimFor(mode))
                return sway * Mathf.Max(0, 1 - AimingAccuracy) / Mathf.Max(1, SightsEfficiency);
            else if (IsSuppressed)
                return sway * SuppressionSwayFactor;
            return sway;
        }

        public bool ShouldAimFor(AimMode mode)
        {
            if (ShooterPawn != null)
            {
                // Check for hunting job
                if (ShooterPawn.CurJob != null && ShooterPawn.CurJob.def == JobDefOf.Hunt)
                    return true;

                // Check for suppression
                if (IsSuppressed) return false;

                // Check for RunAndGun mod
                if (ShooterPawn.pather?.Moving ?? false)
                {
                    return false;
                }
            }
            return mode == AimMode.AimedShot;
        }

        public int ShotsPerBurstFor(FireMode mode)
        {            
            if (CompFireModes != null)
            {
                if (mode == FireMode.SingleFire) return 1;
                if (mode == FireMode.BurstFire && CompFireModes.Props.aimedBurstShotCount > 0) return CompFireModes.Props.aimedBurstShotCount;
            }
            float burstShotCount = VerbPropsCE.burstShotCount;
            if (EquipmentSource != null)
            {
                float modified = EquipmentSource.GetStatValue(CE_StatDefOf.BurstShotCount);
                if (modified > 0)
                    burstShotCount = modified;
            }
            return (int)burstShotCount;
        }

        /// <summary>
        /// Handles activating aim mode at the start of the burst
        /// </summary>
        public override void WarmupComplete()
        {
            var targetDist = (currentTarget.Cell - caster.Position).LengthHorizontal;
            var aimTicks = (int)Mathf.Lerp(AimTicksMin, AimTicksMax, (targetDist / 100));
            if (ShouldAim && !_isAiming)
            {
                if (caster is Building_TurretGunCE turret)
                {
                    turret.burstWarmupTicksLeft += aimTicks;
                    _isAiming = true;
                    return;
                }
                if (ShooterPawn != null)
                {
                    ShooterPawn.stances.SetStance(new Stance_Warmup(aimTicks, currentTarget, this));
                    _isAiming = true;
                    return;
                }
            }

            // Shooty stuff
            base.WarmupComplete();
            _isAiming = false;

            // Award XP
            if (ShooterPawn?.skills != null && currentTarget.Thing is Pawn)
            {
                var time = verbProps.AdjustedFullCycleTime(this, ShooterPawn);
                time += aimTicks.TicksToSeconds();

                var xpPerSec = currentTarget.Thing.HostileTo(ShooterPawn) ? HostileXp : PawnXp;
                xpPerSec *= time;

                ShooterPawn.skills.Learn(SkillDefOf.Shooting, xpPerSec);
            }
        }

        public override void VerbTickCE()
        {
            if (_isAiming)
            {
                if (!ShouldAim)
                {
                    WarmupComplete();
                }
                if (!(caster is Building_TurretGunCE) && ShooterPawn?.stances?.curStance?.GetType() != typeof(Stance_Warmup))
                {
                    _isAiming = false;
                }
            }

            if (isBipodGun && Controller.settings.BipodMechanics)
            {
                EquipmentSource.TryGetComp<BipodComp>().SetUpStart(CasterPawn);
            }
        }

        public virtual ShiftVecReport SimulateShiftVecReportFor(LocalTargetInfo target, AimMode aimMode)
        {
            IntVec3 targetCell = target.Cell;
            ShiftVecReport report = new ShiftVecReport();

            report.target = target;
            report.aimingAccuracy = AimingAccuracy;
            report.sightsEfficiency = SightsEfficiency;
            report.shotDist = (targetCell - caster.Position).LengthHorizontal;
            report.maxRange = EffectiveRange;
            report.lightingShift = CE_Utility.GetLightingShift(Shooter, LightingTracker.CombatGlowAtFor(caster.Position, targetCell));

            if (!caster.Position.Roofed(caster.Map) || !targetCell.Roofed(caster.Map))  //Change to more accurate algorithm?
            {
                report.weatherShift = 1 - caster.Map.weatherManager.CurWeatherAccuracyMultiplier;
            }
            report.shotSpeed = ShotSpeed;
            report.swayDegrees = SwayAmplitudeFor(aimMode);
            float spreadmult = projectilePropsCE != null ? projectilePropsCE.spreadMult : 0f;
            report.spreadDegrees = (EquipmentSource?.GetStatValue(StatDef.Named("ShotSpread")) ?? 0) * spreadmult;
            return report;
        }

        /// <summary>
        /// Reset selected fire mode back to default when gun is dropped
        /// </summary>
        public override void Notify_EquipmentLost()
        {
            base.Notify_EquipmentLost();
            if (CompFireModes != null)
            {
                CompFireModes.ResetModes();
            }
        }

        /// <summary>
        /// Checks to see if enemy is blind before shooting
        /// </summary>
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (ShooterPawn != null && !ShooterPawn.health.capacities.CapableOf(PawnCapacityDefOf.Sight)) return false;
            return base.CanHitTargetFrom(root, targ);
        }

        public override bool TryCastShot()
        {
            //Reduce ammunition
            if (CompAmmo != null)
            {
                if (!CompAmmo.TryReduceAmmoCount(VerbPropsCE.ammoConsumedPerShotCount))
                {
                    return false;
                }
            }
            if (base.TryCastShot())
            {
                //Required since Verb_Shoot does this but Verb_LaunchProjectileCE doesn't when calling base.TryCastShot() because Shoot isn't its base
                if (ShooterPawn != null)
                {
                    ShooterPawn.records.Increment(RecordDefOf.ShotsFired);
                }
                //Drop casings
                if (VerbPropsCE.ejectsCasings && projectilePropsCE.dropsCasings)
                {
                    CE_Utility.ThrowEmptyCasing(caster.DrawPos, caster.Map, DefDatabase<FleckDef>.GetNamed(projectilePropsCE.casingMoteDefname));
                }
                // This needs to here for weapons without magazine to ensure their last shot plays sounds
                if (CompAmmo != null && !CompAmmo.HasMagazine && CompAmmo.UseAmmo)
                {
                    if (!CompAmmo.Notify_ShotFired())
                    {
                        if (VerbPropsCE.muzzleFlashScale > 0.01f)
                        {
                            FleckMakerCE.Static(caster.Position, caster.Map, FleckDefOf.ShotFlash, VerbPropsCE.muzzleFlashScale);
                        }
                        if (VerbPropsCE.soundCast != null)
                        {
                            VerbPropsCE.soundCast.PlayOneShot(new TargetInfo(caster.Position, caster.Map));
                        }
                        if (VerbPropsCE.soundCastTail != null)
                        {
                            VerbPropsCE.soundCastTail.PlayOneShotOnCamera();
                        }
                        if (ShooterPawn != null)
                        {
                            if (ShooterPawn.thinker != null)
                            {
                                ShooterPawn.mindState.lastEngageTargetTick = Find.TickManager.TicksGame;
                            }
                        }
                    }
                    return CompAmmo.Notify_PostShotFired();
                }
                return true;
            }
            return false;
        }
        #endregion
    }
}
