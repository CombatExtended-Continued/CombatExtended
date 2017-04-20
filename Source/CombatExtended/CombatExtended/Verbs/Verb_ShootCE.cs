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

        // XP amounts
        private const float objectXP = 0.1f;
        private const float pawnXP = 0.75f;
        private const float hostileXP = 3.6f;

        #endregion

        #region Fields

        private CompFireModes compFireModes = null;
        private bool isAiming = false;
        private int xpTicks = 0;                        // Tracker to see how much xp should be awarded for time spent aiming + bursting

        #endregion

        #region Properties

        protected override int ShotsPerBurst
        {
            get
            {
                if (this.CompFireModes != null)
                {
                    if (this.CompFireModes.currentFireMode == FireMode.SingleFire)
                    {
                        return 1;
                    }
                    if ((this.CompFireModes.currentFireMode == FireMode.BurstFire || (UseDefaultModes && this.CompFireModes.Props.aiUseBurstMode))
                        && this.CompFireModes.Props.aimedBurstShotCount > 0)
                    {
                        return this.CompFireModes.Props.aimedBurstShotCount;
                    }
                }
                return this.VerbPropsCE.burstShotCount;
            }
        }

        private CompFireModes CompFireModes
        {
            get
            {
                if (this.compFireModes == null && this.ownerEquipment != null)
                {
                    this.compFireModes = this.ownerEquipment.TryGetComp<CompFireModes>();
                }
                return this.compFireModes;
            }
        }

        private bool ShouldAim
        {
            get
            {
                if (CompFireModes != null)
                {
                    if (this.CasterIsPawn)
                    {
                        // Check for hunting job
                        if (CasterPawn.CurJob != null && CasterPawn.CurJob.def == JobDefOf.Hunt)
                            return true;

                        // Check for suppression
                        CompSuppressable comp = this.caster.TryGetComp<CompSuppressable>();
                        if (comp != null && comp.isSuppressed) return false;
                    }
                    return this.CompFireModes.currentAimMode == AimMode.AimedShot || (UseDefaultModes && this.CompFireModes.Props.aiUseAimMode);
                }
                return false;
            }
        }
        
        // Whether this gun should use default AI firing modes
        private bool UseDefaultModes => !(caster.Faction == Faction.OfPlayer);

        protected override float SwayAmplitude
        {
            get
            {
                float sway = base.SwayAmplitude;
                if (ShouldAim) sway *= Mathf.Max(0, 1 - AimingAccuracy);
                return sway;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Handles activating aim mode at the start of the burst
        /// </summary>
        public override void WarmupComplete()
        {
            if (xpTicks <= 0)
                xpTicks = Mathf.CeilToInt(verbProps.warmupTime * 0.5f);

            if (this.ShouldAim && !this.isAiming)
            {
                float targetDist = (this.currentTarget.Cell - this.caster.Position).LengthHorizontal;
                int aimTicks = (int)Mathf.Lerp(aimTicksMin, aimTicksMax, (targetDist / 100));
                if (CasterIsPawn)
                {
                    this.CasterPawn.stances.SetStance(new Stance_Warmup(aimTicks, this.currentTarget, this));
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
                if (CasterIsPawn && this.CasterPawn.stances.curStance.GetType() != typeof(Stance_Warmup))
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
                        if (targetPawn.HostileTo(this.caster.Faction))
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
            if (CasterIsPawn && !CasterPawn.health.capacities.CapableOf(PawnCapacityDefOf.Sight)) return false;
            return base.CanHitTargetFrom(root, targ);
        }
        
        protected override bool TryCastShot()
        {
            //Reduce ammunition
            if (CompAmmo != null)
            {
                if (!CompAmmo.TryReduceAmmoCount())
                {
                    if (CompAmmo.hasMagazine)
                        CompAmmo.TryStartReload();
                    return false;
                }
            }
            if (base.TryCastShot())
            {
                //Drop casings
                if (VerbPropsCE.ejectsCasings && projectilePropsCE.dropsCasings)
                {
                    CE_Utility.ThrowEmptyCasing(caster.DrawPos, caster.Map, ThingDef.Named(projectilePropsCE.casingMoteDefname));
                }
	            // This needs to here for weapons without magazine to ensure their last shot plays sounds
                if (CompAmmo != null && !CompAmmo.hasMagazine && CompAmmo.useAmmo)
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
						if (CasterIsPawn)
						{
							if (CasterPawn.thinker != null)
							{
								CasterPawn.mindState.lastEngageTargetTick = Find.TickManager.TicksGame;
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