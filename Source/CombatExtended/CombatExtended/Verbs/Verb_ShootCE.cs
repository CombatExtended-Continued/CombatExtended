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
        private const int aimTicksMin = 30;
        private const int aimTicksMax = 240;

        // XP amounts  As of A17 non-hostile pawns/objects are (per shot) worth 6xp and hostile pawns are worth 240 xp.
        private const float objectXP = 0.1f;
        private const float pawnXP = 0.75f;
        private const float hostileXP = 3.6f;

        // Suppression aim penalty
        private const float SuppressionSwayFactor = 1.5f;

        #endregion

        #region Fields

        private bool isAiming = false;
        private int xpTicks = 0;                        // Tracker to see how much xp should be awarded for time spent aiming + bursting
        const float BaseXPMultiplyer = 0.5f;            // the amount warmup time is multiplied by for the quickshot fire mode (initial XP)

        #endregion

        #region Properties

        protected override int ShotsPerBurst
        {
            get
            {
                if (this.CompFireModes != null)
                {
                    if (this.CompFireModes.CurrentFireMode == FireMode.SingleFire)
                    {
                        return 1;
                    }
                    if (this.CompFireModes.CurrentFireMode == FireMode.BurstFire
                        && this.CompFireModes.Props.aimedBurstShotCount > 0)
                    {
                        return this.CompFireModes.Props.aimedBurstShotCount;
                    }
                }
                return this.VerbPropsCE.burstShotCount;
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
                    }
                    return this.CompFireModes.CurrentAimMode == AimMode.AimedShot;
                }
                return false;
            }
        }

        protected override float SwayAmplitude
        {
            get
            {
                float sway = base.SwayAmplitude;
                if (ShouldAim) sway = (sway / Mathf.Max(1, EquipmentSource.GetStatValue(CE_StatDefOf.SightsEfficiency))) * Mathf.Max(0, 1 - AimingAccuracy);
                else if (IsSuppressed) sway *= SuppressionSwayFactor;
                return sway;
            }
        }

        // Whether our shooter is currently under suppressive fire
        private bool IsSuppressed => ShooterPawn?.TryGetComp<CompSuppressable>()?.isSuppressed ?? false;

        #endregion

        #region Methods

        /// <summary>
        /// Handles activating aim mode at the start of the burst
        /// </summary>
        public override void WarmupComplete()
        {
            if (xpTicks <= 0)
                xpTicks = Mathf.CeilToInt(verbProps.warmupTime * BaseXPMultiplyer);

            if (this.ShouldAim && !this.isAiming)
            {
                float targetDist = (this.currentTarget.Cell - this.caster.Position).LengthHorizontal;
                int aimTicks = (int)Mathf.Lerp(aimTicksMin, aimTicksMax, (targetDist / 100));
                if (ShooterPawn != null)
                {
                    this.ShooterPawn.stances.SetStance(new Stance_Warmup(aimTicks, this.currentTarget, this));
                    this.isAiming = true;
                    return;
                }
                else
                {
                    Building_TurretGunCE turret = caster as Building_TurretGunCE;
                    if (turret != null)
                    {
                        turret.burstWarmupTicksLeft += aimTicks;
                        this.isAiming = true;
                        return;
                    }
                }
            }

            // Shooty stuff
            base.WarmupComplete();
            this.isAiming = false;
        }

        public override void VerbTickCE()
        {
            if (this.isAiming)
            {
                this.xpTicks++;
                if (!this.ShouldAim)
                {
                    this.WarmupComplete();
                }
                if (ShooterPawn != null && this.ShooterPawn.stances.curStance?.GetType() != typeof(Stance_Warmup))
                {
                    this.isAiming = false;
                }
            }
            // Increase shootTicks while bursting so we can calculate XP afterwards
            else if (this.state == VerbState.Bursting)
            {
                this.xpTicks++;
            }
            else if (this.xpTicks > 0)
            {
                // Reward XP to shooter pawn
                if (this.ShooterPawn != null && this.ShooterPawn.skills != null)
                {
                    float xpPerTick = objectXP;
                    Pawn targetPawn = this.currentTarget.Thing as Pawn;
                    if (targetPawn != null)
                    {
                        if (targetPawn.HostileTo(Shooter.Faction))
                        {
                            xpPerTick = hostileXP;
                        }
                        else
                        {
                            xpPerTick = pawnXP;
                        }
                    }
                    this.ShooterPawn.skills.Learn(SkillDefOf.Shooting, Mathf.Max(xpPerTick * xpTicks, 1));
                }
                this.xpTicks = 0;
            }
        }

        /// <summary>
        /// Reset selected fire mode back to default when gun is dropped
        /// </summary>
        public override void Notify_EquipmentLost()
        {
            base.Notify_EquipmentLost();
            if (this.CompFireModes != null)
            {
                this.CompFireModes.ResetModes();
            }
            caster = null;
        }

        /// <summary>
        /// Checks to see if enemy is blind before shooting
        /// </summary>
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (ShooterPawn != null && !ShooterPawn.health.capacities.CapableOf(PawnCapacityDefOf.Sight)) return false;
            return base.CanHitTargetFrom(root, targ);
        }
        
        protected override bool TryCastShot()
        {
            //Reduce ammunition
            if (CompAmmo != null)
            {
                if (!CompAmmo.TryReduceAmmoCount())
                {
                    if (CompAmmo.HasMagazine)
                    {
                        CompAmmo.TryStartReload();
                    }
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
                    CE_Utility.ThrowEmptyCasing(caster.DrawPos, caster.Map, ThingDef.Named(projectilePropsCE.casingMoteDefname));
                }
	            // This needs to here for weapons without magazine to ensure their last shot plays sounds
                if (CompAmmo != null && !CompAmmo.HasMagazine && CompAmmo.UseAmmo)
                {
                	if (!CompAmmo.Notify_ShotFired())
                	{
						if (VerbPropsCE.muzzleFlashScale > 0.01f)
						{
							MoteMaker.MakeStaticMote(caster.Position, caster.Map, ThingDefOf.Mote_ShotFlash, VerbPropsCE.muzzleFlashScale);
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
