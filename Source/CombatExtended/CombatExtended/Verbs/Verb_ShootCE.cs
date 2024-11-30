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

        public Vector3 drawPos;

        private CompAmmoUser compAmmo;

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

        protected virtual bool ShouldAim
        {
            get
            {
                if (CompFireModes != null)
                {
                    if (ShooterPawn != null)
                    {
                        // Check for hunting job
                        if (ShooterPawn.CurJob != null && ShooterPawn.CurJob.def == JobDefOf.Hunt)
                        {
                            return true;
                        }

                        // Check for suppression
                        if (IsSuppressed)
                        {
                            return false;
                        }

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

        public override float SwayAmplitude // TODO: Fix SwayAmplitude and SwayAmplitudeFor code re-use
        {
            get
            {
                var sway = base.SwayAmplitude;
                float sightsEfficiency = SightsEfficiency;
                if (ShooterPawn != null && !ShooterPawn.health.capacities.CapableOf(PawnCapacityDefOf.Sight))
                {
                    sightsEfficiency = 0;
                }
                if (ShouldAim)
                {
                    return sway * Mathf.Max(0, 1 - AimingAccuracy) / Mathf.Max(1, sightsEfficiency);
                }
                else if (IsSuppressed)
                {
                    return sway * SuppressionSwayFactor;
                }
                return sway;
            }
        }

        public float AimAngle
        {
            get
            {
                if (this.CurrentTarget == null)
                {
                    return 143f;
                }
                Vector3 vector = (CurrentTarget.Thing == null ? CurrentTarget.Cell.ToVector3Shifted() : CurrentTarget.Thing.DrawPos);
                float num = 143f;
                if ((vector - caster.DrawPos).MagnitudeHorizontalSquared() > 0.001f)
                {
                    num = (vector - caster.DrawPos).AngleFlat();
                }
                return num;
            }
        }

        // Whether our shooter is currently under suppressive fire
        private bool IsSuppressed => ShooterPawn?.TryGetComp<CompSuppressable>()?.isSuppressed ?? false;

        public CompAmmoUser CompAmmo
        {
            get
            {
                compAmmo ??= EquipmentSource?.TryGetComp<CompAmmoUser>();
                return compAmmo;
            }
        }

        public override ThingDef Projectile => CompAmmo?.CurrentAmmo != null ? CompAmmo.CurAmmoProjectile : base.Projectile;

        #endregion

        #region Methods

        public float SwayAmplitudeFor(AimMode mode)
        {
            float sway = base.SwayAmplitude;
            float sightsEfficiency = SightsEfficiency;
            if (ShooterPawn != null && !ShooterPawn.health.capacities.CapableOf(PawnCapacityDefOf.Sight))
            {
                sightsEfficiency = 0;
            }
            if (ShouldAimFor(mode))
            {
                return sway * Mathf.Max(0, 1 - AimingAccuracy) / Mathf.Max(1, sightsEfficiency);
            }
            else if (IsSuppressed)
            {
                return sway * SuppressionSwayFactor;
            }
            return sway;
        }

        public bool ShouldAimFor(AimMode mode)
        {
            if (ShooterPawn != null)
            {
                // Check for hunting job
                if (ShooterPawn.CurJob != null && ShooterPawn.CurJob.def == JobDefOf.Hunt)
                {
                    return true;
                }

                // Check for suppression
                if (IsSuppressed)
                {
                    return false;
                }

                // Check for RunAndGun mod
                if (ShooterPawn.pather?.Moving ?? false)
                {
                    return false;
                }
            }
            return mode == AimMode.AimedShot;
        }

        public virtual int ShotsPerBurstFor(FireMode mode)
        {
            if (CompFireModes != null)
            {
                if (mode == FireMode.SingleFire)
                {
                    return 1;
                }
                if (mode == FireMode.BurstFire && CompFireModes.Props.aimedBurstShotCount > 0)
                {
                    return CompFireModes.Props.aimedBurstShotCount;
                }
            }
            float burstShotCount = VerbPropsCE.burstShotCount;
            if (EquipmentSource != null && (!EquipmentSource.TryGetComp<CompUnderBarrel>()?.usingUnderBarrel ?? false))
            {
                float modified = EquipmentSource.GetStatValue(CE_StatDefOf.BurstShotCount);
                if (modified > 0)
                {
                    burstShotCount = modified;
                }
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
                    RecalculateWarmupTicks();
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

        public override bool Available()
        {
            if (!base.Available())
            {
                return false;
            }

            // Add check for reload
            bool isAttacking = ShooterPawn?.CurJobDef == JobDefOf.AttackStatic || WarmingUp;
            if (isAttacking && !(CompAmmo?.CanBeFiredNow ?? true))
            {
                CompAmmo?.TryStartReload();
                resetRetarget();
                return false;
            }

            return true;
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

        /// <summary>
        /// Checks to see if enemy is blind before shooting
        /// </summary>
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (ShooterPawn != null && !ShooterPawn.health.capacities.CapableOf(PawnCapacityDefOf.Sight))
            {
                if (!ShooterPawn.health.capacities.CapableOf(PawnCapacityDefOf.Hearing))
                {
                    // blind and deaf;
                    return false;
                }
                // blind but not deaf
                float dist = targ.Cell.DistanceTo(root);
                if (dist < 5f)
                {
                    return base.CanHitTargetFrom(root, targ);
                }
                Map map = ShooterPawn.Map;
                LightingTracker tracker = map.GetLightingTracker();
                float glow = tracker.GetGlowForCell(targ.Cell);
                if (glow / dist < 0.1f)
                {
                    return false;
                }
            }
            return base.CanHitTargetFrom(root, targ);
        }

        public override void RecalculateWarmupTicks()
        {
            if (!Controller.settings.FasterRepeatShots)
            {
                return;
            }
            Vector3 u = caster.TrueCenter();
            Vector3 v = currentTarget.Thing?.TrueCenter() ?? currentTarget.Cell.ToVector3Shifted();
            if (currentTarget.Pawn is Pawn dtPawn)
            {
                v += dtPawn.Drawer.leaner.LeanOffset * 0.5f;
            }

            var d = v - u;
            var newShotRotation = (-90 + Mathf.Rad2Deg * Mathf.Atan2(d.z, d.x)) % 360;
            var delta = Mathf.Abs(newShotRotation - lastShotRotation) + lastRecoilDeg;
            lastRecoilDeg = 0;
            var maxReduction = storedShotReduction ?? (CompFireModes?.CurrentAimMode == AimMode.SuppressFire ?
                                                       0.1f :
                                                       (_isAiming ? 0.5f : 0.25f));
            var reduction = Mathf.Max(maxReduction, delta / 45f);
            storedShotReduction = reduction;

            if (reduction < 1.0f)
            {
                this.BurstWarmupTicksLeft = (int)(BurstWarmupTicksLeft * reduction);
            }

        }

        //For revolvers and break actions. Intended to be called by compammouser on reload
        public void ExternalCallDropCasing(int randomSeedOffset = -1)
        {
            bool fromPawn = false;
            GunDrawExtension ext = EquipmentSource?.def.GetModExtension<GunDrawExtension>();
            if (ShooterPawn != null)
            {
                fromPawn = drawPos != Vector3.zero;
            }
            //No aim angle because casing eject happens when pawn lowers its gun to reload
            CE_Utility.GenerateAmmoCasings(projectilePropsCE, fromPawn ? drawPos : caster.DrawPos, caster.Map, 0, VerbPropsCE.recoilAmount, fromPawn: fromPawn, extension: ext, randomSeedOffset);
        }

        public override bool TryCastShot()
        {
            if (!CompAmmo?.TryPrepareShot() ?? false)
            {
                return false;
            }
            if (base.TryCastShot())
            {
                return OnCastSuccessful();
            }
            return false;
        }
        protected virtual bool OnCastSuccessful()
        {
            bool fromPawn = false;
            GunDrawExtension ext = EquipmentSource?.def.GetModExtension<GunDrawExtension>();
            //Required since Verb_Shoot does this but Verb_LaunchProjectileCE doesn't when calling base.TryCastShot() because Shoot isn't its base
            if (ShooterPawn != null)
            {
                ShooterPawn.records.Increment(RecordDefOf.ShotsFired);
                fromPawn = drawPos != Vector3.zero;
            }

            //Drop casings
            if (VerbPropsCE.ejectsCasings && (!ext?.DropCasingWhenReload ?? true))
            {
                CE_Utility.GenerateAmmoCasings(projectilePropsCE, fromPawn ? drawPos : caster.DrawPos, caster.Map, AimAngle, VerbPropsCE.recoilAmount, fromPawn: fromPawn, extension: ext);
            }

            if (CompAmmo == null)
            {
                return true;
            }

            int ammoConsumedPerShot = (CompAmmo.Props.ammoSet?.ammoConsumedPerShot ?? 1) * VerbPropsCE.ammoConsumedPerShotCount;
            CompAmmo.Notify_ShotFired(ammoConsumedPerShot);

            if (ShooterPawn != null && !CompAmmo.CanBeFiredNow)
            {
                CompAmmo.TryStartReload();
                resetRetarget();
            }

            // This needs to here for weapons without magazine to ensure their last shot plays sounds
            if (!CompAmmo.HasMagazine && CompAmmo.UseAmmo)
            {
                if (!CompAmmo.HasAmmoOrMagazine)
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
        #endregion
    }
}
