﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using Verse.Grammar;
using UnityEngine;

namespace CombatExtended
{
    public class Verb_LaunchProjectileCE : Verb
    {
        #region Constants

        // Cover check constants
        private const float distToCheckForCover = 3f;   // How many cells to raycast on the cover check
        private const float segmentLength = 0.2f;       // How long a single raycast segment is
        //private const float shotHeightFactor = 0.85f;   // The height at which pawns hold their guns

        #endregion

        #region Fields

        // Targeting factors
        private float estimatedTargDist = -1;           // Stores estimate target distance for each burst, so each burst shot uses the same
        private int numShotsFired = 0;                  // Stores how many shots were fired for purposes of recoil

        // Angle in Vector2(degrees, radians)
        private Vector2 newTargetLoc = new Vector2(0, 0);
        private Vector2 sourceLoc = new Vector2(0, 0);

        private float shotAngle = 0f;   // Shot angle off the ground in radians.
        private float shotRotation = 0f;    // Angle rotation towards target.

        protected CompCharges compCharges = null;
        protected CompAmmoUser compAmmo = null;
        protected CompFireModes compFireModes = null;
        protected CompChangeableProjectile compChangeable = null;
        protected CompReloadable compReloadable = null;
        private float shotSpeed = -1;

        private float rotationDegrees = 0f;
        private float angleRadians = 0f;

        //private int lastTauntTick;

        #endregion

        #region Properties

        public VerbPropertiesCE VerbPropsCE => verbProps as VerbPropertiesCE;
        public ProjectilePropertiesCE projectilePropsCE => Projectile.projectile as ProjectilePropertiesCE;

        // Returns either the pawn aiming the weapon or in case of turret guns the turret operator or null if neither exists
        public Pawn ShooterPawn => CasterPawn ?? CE_Utility.TryGetTurretOperator(caster);
        public Thing Shooter => ShooterPawn ?? caster;

        protected CompCharges CompCharges
        {
            get
            {
                if (compCharges == null && EquipmentSource != null)
                {
                    compCharges = EquipmentSource.TryGetComp<CompCharges>();
                }
                return compCharges;
            }
        }
        private float ShotSpeed
        {
            get
            {
                if (CompCharges != null)
                {
                    if (CompCharges.GetChargeBracket((currentTarget.Cell - caster.Position).LengthHorizontal, ShotHeight, projectilePropsCE.Gravity, out var bracket))
                    {
                        shotSpeed = bracket.x;
                    }
                }
                else
                {
                    shotSpeed = Projectile.projectile.speed;
                }
                return shotSpeed;
            }
        }
        protected float ShotHeight => (new CollisionVertical(caster)).shotHeight;
        private Vector3 ShotSource
        {
            get
            {
                var casterPos = caster.DrawPos;
                return new Vector3(casterPos.x, ShotHeight, casterPos.z);
            }
        }

        protected float ShootingAccuracy => Mathf.Min(CasterShootingAccuracyValue(caster), 4.5f);
        protected float AimingAccuracy => Mathf.Min(Shooter.GetStatValue(CE_StatDefOf.AimingAccuracy), 1.5f); //equivalent of ShooterPawn?.GetStatValue(CE_StatDefOf.AimingAccuracy) ?? caster.GetStatValue(CE_StatDefOf.AimingAccuracy)
        protected float SightsEfficiency => EquipmentSource?.GetStatValue(CE_StatDefOf.SightsEfficiency) ?? 1f;
        protected virtual float SwayAmplitude => Mathf.Max(0, (4.5f - ShootingAccuracy) * (EquipmentSource?.GetStatValue(StatDef.Named("SwayFactor")) ?? 1f));

        // Ammo variables
        protected CompAmmoUser CompAmmo
        {
            get
            {
                if (compAmmo == null && EquipmentSource != null)
                {
                    compAmmo = EquipmentSource.TryGetComp<CompAmmoUser>();
                }
                return compAmmo;
            }
        }
        public ThingDef Projectile
        {
            get
            {
                if (CompAmmo != null && CompAmmo.CurrentAmmo != null)
                {
                    return CompAmmo.CurAmmoProjectile;
                }
                if (CompChangeable != null && CompChangeable.Loaded)
                {
                    return CompChangeable.Projectile;
                }
                return VerbPropsCE.defaultProjectile;
            }
        }

        protected CompChangeableProjectile CompChangeable
        {
            get
            {
                if (compChangeable == null && EquipmentSource != null)
                {
                    compChangeable = EquipmentSource.TryGetComp<CompChangeableProjectile>();
                }
                return compChangeable;
            }
        }

        public CompFireModes CompFireModes
        {
            get
            {
                if (compFireModes == null && EquipmentSource != null)
                {
                    compFireModes = EquipmentSource.TryGetComp<CompFireModes>();
                }
                return compFireModes;
            }
        }

        public CompReloadable CompReloadable
        {
            get
            {
                if (compReloadable == null && EquipmentSource != null)
                {
                    compReloadable = EquipmentSource.TryGetComp<CompReloadable>();
                }
                return compReloadable;
            }
        }

        private bool IsAttacking => ShooterPawn?.CurJobDef == JobDefOf.AttackStatic || WarmingUp;


        #endregion

        #region Methods

        public override bool Available()
        {
            // This part copied from vanilla Verb_LaunchProjectile
            if (!base.Available())
                return false;

            if (CasterIsPawn)
            {
                if (CasterPawn.Faction != Faction.OfPlayer
                    && CasterPawn.mindState.MeleeThreatStillThreat
                    && CasterPawn.mindState.meleeThreat.AdjacentTo8WayOrInside(CasterPawn))
                    return false;
            }

            // Add check for reload
            if (Projectile == null || (IsAttacking && CompAmmo != null && !CompAmmo.CanBeFiredNow))
            {
                CompAmmo?.TryStartReload();
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets caster's weapon handling based on if it's a pawn or a turret
        /// </summary>
        /// <param name="caster">What thing is equipping the projectile launcher</param>
        private float CasterShootingAccuracyValue(Thing caster) => // ShootingAccuracy was split into ShootingAccuracyPawn and ShootingAccuracyTurret
            (caster as Pawn != null) ? caster.GetStatValue(StatDefOf.ShootingAccuracyPawn) : caster.GetStatValue(StatDefOf.ShootingAccuracyTurret);

        /// <summary>
        /// Resets current burst shot count and estimated distance at beginning of the burst
        /// </summary>
        public override void WarmupComplete()
        {
            if (ShooterPawn != null && ShooterPawn.pather == null)
            {
                return; //Pawn has started a jump pack animation, or otherwise became despawned temporarily
            }
            // attack shooting expression
            if ((ShooterPawn?.Spawned ?? false) && currentTarget.Thing is Pawn && Rand.Chance(0.25f))
            {
                var tauntThrower = (TauntThrower)ShooterPawn.Map.GetComponent(typeof(TauntThrower));
                tauntThrower?.TryThrowTaunt(CE_RulePackDefOf.AttackMote, ShooterPawn);
            }

            numShotsFired = 0;
            base.WarmupComplete();
            Find.BattleLog.Add(
                new BattleLogEntry_RangedFire(
                    Shooter,
                    (!currentTarget.HasThing) ? null : currentTarget.Thing,
                    (EquipmentSource == null) ? null : EquipmentSource.def,
                    Projectile,
                    VerbPropsCE.burstShotCount > 1)
            );
        }

        /// <summary>
        /// Shifts the original target position in accordance with target leading, range estimation and weather/lighting effects
        /// </summary>
        protected virtual void ShiftTarget(ShiftVecReport report, bool calculateMechanicalOnly = false, bool isInstant = false)
        {
            if (!calculateMechanicalOnly)
            {
                Vector3 u = caster.TrueCenter();
                sourceLoc.Set(u.x, u.z);

                if (numShotsFired == 0)
                {
                    // On first shot of burst do a range estimate
                    estimatedTargDist = report.GetRandDist();
                }
                Vector3 v = report.target.Thing?.TrueCenter() ?? report.target.Cell.ToVector3Shifted(); //report.targetPawn != null ? report.targetPawn.DrawPos + report.targetPawn.Drawer.leaner.LeanOffset * 0.5f : report.target.Cell.ToVector3Shifted();
                if (report.targetPawn != null)
                    v += report.targetPawn.Drawer.leaner.LeanOffset * 0.5f;

                newTargetLoc.Set(v.x, v.z);

                // ----------------------------------- STEP 1: Actual location + Shift for visibility

                //FIXME : GetRandCircularVec may be causing recoil to be unnoticeable - each next shot in the burst has a new random circular vector around the target.
                newTargetLoc += report.GetRandCircularVec();

                // ----------------------------------- STEP 2: Estimated shot to hit location

                newTargetLoc = sourceLoc + (newTargetLoc - sourceLoc).normalized * estimatedTargDist;

                // Lead a moving target
                if (!isInstant) {

                    newTargetLoc += report.GetRandLeadVec();
                }

                // ----------------------------------- STEP 3: Recoil, Skewing, Skill checks, Cover calculations

                rotationDegrees = 0f;
                angleRadians = 0f;

                GetSwayVec(ref rotationDegrees, ref angleRadians);
                GetRecoilVec(ref rotationDegrees, ref angleRadians);

                // Height difference calculations for ShotAngle
                float targetHeight = 0f;

                var coverRange = new CollisionVertical(report.cover).HeightRange;   //Get " " cover, assume it is the edifice

                // Projectiles with flyOverhead target the surface in front of the target
                if (Projectile.projectile.flyOverhead)
                {
                    targetHeight = coverRange.max;
                }
                else
                {
                    var victimVert = new CollisionVertical(currentTarget.Thing);
                    var targetRange = victimVert.HeightRange;   //Get lower and upper heights of the target
                    if (targetRange.min < coverRange.max)   //Some part of the target is hidden behind some cover
                    {
                        // - It is possible for targetRange.max < coverRange.max, technically, in which case the shooter will never hit until the cover is gone.
                        // - This should be checked for in LoS -NIA
                        targetRange.min = coverRange.max;

                        // Target fully hidden, shift aim upwards if we're doing suppressive fire
                        if (targetRange.max <= coverRange.max && CompFireModes?.CurrentAimMode == AimMode.SuppressFire)
                        {
                            targetRange.max = coverRange.max * 2;
                        }
                    }
                    else if (currentTarget.Thing is Pawn)
                    {
                        // Aim for center of mass on an exposed target
                        targetRange.min = victimVert.BottomHeight;
                        targetRange.max = victimVert.MiddleHeight;
                    }
                    targetHeight = VerbPropsCE.ignorePartialLoSBlocker ? 0 : targetRange.Average;
                }
                if (projectilePropsCE.isInstant) {
		    angleRadians += Mathf.Atan2(targetHeight - ShotHeight, (newTargetLoc - sourceLoc).magnitude);
                }
                else {
                    angleRadians += ProjectileCE.GetShotAngle(ShotSpeed, (newTargetLoc - sourceLoc).magnitude, targetHeight - ShotHeight, Projectile.projectile.flyOverhead, projectilePropsCE.Gravity);
                }
            }

            // ----------------------------------- STEP 4: Mechanical variation

            // Get shotvariation, in angle Vector2 RADIANS.
            Vector2 spreadVec = (projectilePropsCE.isInstant && projectilePropsCE.damageFalloff)? new Vector2(0,0) : report.GetRandSpreadVec() ;
            // ----------------------------------- STEP 5: Finalization

            var w = (newTargetLoc - sourceLoc);
            shotRotation = (-90 + Mathf.Rad2Deg * Mathf.Atan2(w.y, w.x) + rotationDegrees + spreadVec.x) % 360;
            shotAngle = angleRadians + spreadVec.y * Mathf.Deg2Rad;
        }

        /// <summary>
        /// Calculates the amount of recoil at a given point in a burst, up to a maximum
        /// </summary>
        /// <param name="rotation">The ref float to have horizontal recoil in degrees added to.</param>
        /// <param name="angle">The ref float to have vertical recoil in radians added to.</param>
        private void GetRecoilVec(ref float rotation, ref float angle)
        {
            var recoil = VerbPropsCE.recoilAmount;
            float maxX = recoil * 0.5f;
            float minX = -maxX;
            float maxY = recoil;
            float minY = -recoil / 3;

            float recoilMagnitude = numShotsFired == 0 ? 0 : Mathf.Pow((5 - ShootingAccuracy), (Mathf.Min(10, numShotsFired) / 6.25f));

            rotation += recoilMagnitude * UnityEngine.Random.Range(minX, maxX);
            angle += Mathf.Deg2Rad * recoilMagnitude * UnityEngine.Random.Range(minY, maxY);
        }

        /// <summary>
        /// Calculates current weapon sway based on a parametric function with maximum amplitude depending on shootingAccuracy and scaled by weapon's swayFactor.
        /// </summary>
        /// <param name="rotation">The ref float to have horizontal sway in degrees added to.</param>
        /// <param name="angle">The ref float to have vertical sway in radians added to.</param>
        protected void GetSwayVec(ref float rotation, ref float angle)
        {
            float ticks = (float)(Find.TickManager.TicksAbs + Shooter.thingIDNumber);
            rotation += SwayAmplitude * (float)Mathf.Sin(ticks * 0.022f);
            angle += Mathf.Deg2Rad * 0.25f * SwayAmplitude * (float)Mathf.Sin(ticks * 0.0165f);
        }

        public virtual ShiftVecReport ShiftVecReportFor(LocalTargetInfo target)
        {
            IntVec3 targetCell = target.Cell;
            ShiftVecReport report = new ShiftVecReport();
            report.target = target;
            report.aimingAccuracy = AimingAccuracy;
            report.sightsEfficiency = SightsEfficiency;
            report.shotDist = (targetCell - caster.Position).LengthHorizontal;
            report.maxRange = verbProps.range;

            report.lightingShift = 1 - caster.Map.glowGrid.GameGlowAt(targetCell);
            if (!caster.Position.Roofed(caster.Map) || !targetCell.Roofed(caster.Map))  //Change to more accurate algorithm?
            {
                report.weatherShift = 1 - caster.Map.weatherManager.CurWeatherAccuracyMultiplier;
            }
            report.shotSpeed = ShotSpeed;
            report.swayDegrees = SwayAmplitude;
            var spreadmult = projectilePropsCE != null ? projectilePropsCE.spreadMult : 0f;
            report.spreadDegrees = (EquipmentSource?.GetStatValue(StatDef.Named("ShotSpread")) ?? 0) * spreadmult;
            Thing cover;
            float smokeDensity;
            GetHighestCoverAndSmokeForTarget(target, out cover, out smokeDensity);
            report.cover = cover;
            report.smokeDensity = smokeDensity;

            return report;
        }

        public float AdjustShotHeight(Thing caster, LocalTargetInfo target, ref float shotHeight)
        {
            /* TODO:  This really should determine how much the shooter needs to rise up for a *good* shot.  
               If we're shooting at something tall, we might not need to rise at all, if we're shooting at 
               something short, we might need to rise *more* than just above the cover.  This at least handles 
               cases where we're below cover, but the taret is taller than the cover */
            GetHighestCoverAndSmokeForTarget(target, out Thing cover, out float smoke);
            var shooterHeight = CE_Utility.GetBoundsFor(caster).max.y;
            var coverHeight = CE_Utility.GetBoundsFor(cover).max.y;
            var centerOfVisibleTarget = (CE_Utility.GetBoundsFor(target.Thing).max.y - coverHeight) / 2 + coverHeight;
            if (centerOfVisibleTarget > shotHeight)
            {
                if (centerOfVisibleTarget > shooterHeight)
                {
                    centerOfVisibleTarget = shooterHeight;
                }
		float distance = target.Thing.Position.DistanceTo(caster.Position);
                // float wobble = Mathf.Atan2(UnityEngine.Random.Range(shotHeight-centerOfVisibleTarget, centerOfVisibleTarget - shotHeight), distance);
		float triangleHeight = centerOfVisibleTarget - shotHeight;
		float wobble = -Mathf.Atan2(triangleHeight, distance);
		// TODO: Add inaccuracy for not standing in as natural a position
                shotHeight = centerOfVisibleTarget;
                return wobble;
            }
            return 0;
        }

        /// <summary>
        /// Checks for cover along the flight path of the bullet, doesn't check for walls or trees, only intended for cover with partial fillPercent
        /// </summary>
        /// <param name="target">The target of which to find cover of</param>
        /// <param name="cover">Output parameter, filled with the highest cover object found</param>
        /// <returns>True if cover was found, false otherwise</returns>
        private bool GetHighestCoverAndSmokeForTarget(LocalTargetInfo target, out Thing cover, out float smokeDensity)
        {
            Map map = caster.Map;
            Thing targetThing = target.Thing;
            Thing highestCover = null;
            float highestCoverHeight = 0f;

            smokeDensity = 0;

            // Iterate through all cells on line of sight and check for cover and smoke
            var cells = GenSight.PointsOnLineOfSight(target.Cell, caster.Position).ToArray();
            if (cells.Length < 3)
            {
                cover = null;
                return false;
            }
	    bool instant = false;
	    if (Projectile.projectile is ProjectilePropertiesCE pprop)
	    {
		instant = pprop.isInstant;
	    }
	    int endCell = instant? cells.Length : cells.Length / 2;
	    
            for (int i = 0; i < endCell; i++)
            {
                var cell = cells[i];

                if (cell.AdjacentTo8Way(caster.Position)) continue;

                // Check for smoke
                var gas = cell.GetGas(map);
                if (gas != null)
                {
                    smokeDensity += gas.def.gas.accuracyPenalty;
                }

                // Check for cover in the second half of LoS
                if (instant || i <= cells.Length / 2)
                {
                    Pawn pawn = cell.GetFirstPawn(map);
                    Thing newCover = pawn == null ? cell.GetCover(map) : pawn;
		    float newCoverHeight = new CollisionVertical(newCover).Max;

                    // Cover check, if cell has cover compare collision height and get the highest piece of cover, ignore if cover is the target (e.g. solar panels, crashed ship, etc)
                    if (newCover != null
                        && (targetThing == null || !newCover.Equals(targetThing))
                        && (highestCover == null || highestCoverHeight < newCoverHeight)
                        && newCover.def.Fillage == FillCategory.Partial
                        && !newCover.IsPlant())
                    {
                        highestCover = newCover;
                        highestCoverHeight = newCoverHeight;
                        if (Controller.settings.DebugDrawTargetCoverChecks) map.debugDrawer.FlashCell(cell, highestCoverHeight, highestCoverHeight.ToString());
                    }
                }
            }
            cover = highestCover;

            //Report success if found cover
            return cover != null;
        }

        /// <summary>
        /// Checks if the shooter can hit the target from a certain position with regards to cover height
        /// </summary>
        /// <param name="root">The position from which to check</param>
        /// <param name="targ">The target to check for line of sight</param>
        /// <returns>True if shooter can hit target from root position, false otherwise</returns>
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            string unused;
            return CanHitTargetFrom(root, targ, out unused);
        }

        public bool CanHitTarget(LocalTargetInfo targ, out string report)
        {
            return CanHitTargetFrom(caster.Position, targ, out report);
        }

        public virtual bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ, out string report)
        {
            report = "";
            if (caster?.Map == null || !targ.Cell.InBounds(caster.Map) || !root.InBounds(caster.Map))
            {
                report = "Out of bounds";
                return false;
            }
            // Check target self
            if (targ.Thing != null && targ.Thing == caster)
            {
                if (!verbProps.targetParams.canTargetSelf)
                {
                    report = "Can't target self";
                    return false;
                }
                return true;
            }
            // Check thick roofs
            if (Projectile.projectile.flyOverhead)
            {
                RoofDef roofDef = caster.Map.roofGrid.RoofAt(targ.Cell);
                if (roofDef != null && roofDef.isThickRoof)
                {
                    report = "Blocked by roof";
                    return false;
                }
            }
            if (ShooterPawn != null)
            {
                // Check for capable of violence
                if (ShooterPawn.story != null && ShooterPawn.WorkTagIsDisabled(WorkTags.Violent))
                {
                    report = "IsIncapableOfViolenceLower".Translate(ShooterPawn.Name.ToStringShort);
                    return false;
                }

                // Check for apparel
                bool isTurretOperator = caster.def.building?.IsTurret ?? false;
                if (ShooterPawn.apparel != null)
                {
                    List<Apparel> wornApparel = ShooterPawn.apparel.WornApparel;
                    foreach (Apparel current in wornApparel)
                    {
                        //pawns can use turrets while wearing shield belts, but the shield is disabled for the duration via Harmony patch (see Harmony-ShieldBelt.cs)
                        if (!current.AllowVerbCast(root, caster.Map, targ, this) && !(current is ShieldBelt && isTurretOperator))
                        {
                            report = "Shooting disallowed by " + current.LabelShort;
                            return false;
                        }
                    }
                }
            }
            // Check for line of sight
            ShootLine shootLine;
            if (!TryFindCEShootLineFromTo(root, targ, out shootLine))
            {
                float lengthHorizontalSquared = (root - targ.Cell).LengthHorizontalSquared;
                if (lengthHorizontalSquared > verbProps.range * verbProps.range)
                {
                    report = "Out of range";
                }
                else if (lengthHorizontalSquared < verbProps.minRange * verbProps.minRange)
                {
                    report = "Within minimum range";
                }
                else
                {
                    report = "No line of sight";
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// Fires a projectile using the new aiming system
        /// </summary>
        /// <returns>True for successful shot, false otherwise</returns>
        protected override bool TryCastShot()
        {
            if (!TryFindCEShootLineFromTo(caster.Position, currentTarget, out var shootLine))
            {
                return false;
            }
            if (projectilePropsCE.pelletCount < 1)
            {
                Log.Error(EquipmentSource.LabelCap + " tried firing with pelletCount less than 1.");
                return false;
            }
            bool instant = false;

            float spreadDegrees = 0;
            float aperatureSize = 0;

            if (Projectile.projectile is ProjectilePropertiesCE pprop) {
                instant = pprop.isInstant;
                spreadDegrees = (EquipmentSource?.GetStatValue(StatDef.Named("ShotSpread")) ?? 0) * pprop.spreadMult;
                aperatureSize = 0.03f;
            }

            ShiftVecReport report = ShiftVecReportFor(currentTarget);
            bool pelletMechanicsOnly = false;
            for (int i = 0; i < projectilePropsCE.pelletCount; i++)
            {

                ProjectileCE projectile = (ProjectileCE)ThingMaker.MakeThing(Projectile, null);
                GenSpawn.Spawn(projectile, shootLine.Source, caster.Map);
                ShiftTarget(report, pelletMechanicsOnly, instant);

                //New aiming algorithm
                projectile.canTargetSelf = false;

                var targetDistance = (sourceLoc - currentTarget.Cell.ToIntVec2.ToVector2Shifted()).magnitude;
                projectile.minCollisionDistance = Mathf.Min(1.5f, targetDistance * 0.75f);
                projectile.intendedTarget = currentTarget.Thing;
                projectile.mount = caster.Position.GetThingList(caster.Map).FirstOrDefault(t => t is Pawn && t != caster);
                projectile.AccuracyFactor = report.accuracyFactor * report.swayDegrees * ((numShotsFired + 1) * 0.75f);
                if (instant) {
                    var shotHeight = ShotHeight;
                    float tsa = AdjustShotHeight(caster, currentTarget, ref shotHeight);
                    projectile.RayCast(
                                       Shooter,
                                       verbProps,
                                       sourceLoc,
                                       shotAngle + tsa,
                                       shotRotation,
                                       shotHeight,
                                       ShotSpeed,
                                       spreadDegrees,
                                       aperatureSize,
                                       EquipmentSource);

                }
                else  {
                    projectile.Launch(
                                      Shooter,    //Shooter instead of caster to give turret operators' records the damage/kills obtained
                                      sourceLoc,
                                      shotAngle,
                                      shotRotation,
                                      ShotHeight,
                                      ShotSpeed,
                                      EquipmentSource
                                      );
                }
                pelletMechanicsOnly = true;
            }
            /// Log.Message("Fired from "+caster.ThingID+" at "+ShotHeight); /// 
            pelletMechanicsOnly = false;
            numShotsFired++;
            if (CompAmmo != null && !CompAmmo.CanBeFiredNow)
            {
                CompAmmo?.TryStartReload();
            }
            if (CompReloadable != null)
            {
                CompReloadable.UsedOnce();
            }
            return true;
        }

        /// <summary>
        /// This is a custom CE ticker. Since the vanilla VerbTick() method is non-virtual we need to detour VerbTracker and make it call this method in addition to the vanilla ticker in order to
        /// add custom ticker functionality.
        /// </summary>
        public virtual void VerbTickCE()
        {
        }

        #region Line of Sight Utility

        /* Line of sight calculating methods
         * 
         * Copied from vanilla Verse.Verb class, the only change here is usage of our own validator for partial cover checks. Copy-paste should be kept up to date with vanilla
         * and if possible replaced with a cleaner solution.
         * 
         * -NIA
         */

        private List<IntVec3> tempLeanShootSources = new List<IntVec3>();

        public bool TryFindCEShootLineFromTo(IntVec3 root, LocalTargetInfo targ, out ShootLine resultingLine)
        {
            if (targ.HasThing && targ.Thing.Map != caster.Map)
            {
                resultingLine = default(ShootLine);
                return false;
            }
            if (verbProps.range <= ShootTuning.MeleeRange) // If this verb has a MAX range up to melee range (NOT a MIN RANGE!)
            {
                resultingLine = new ShootLine(root, targ.Cell);
                return ReachabilityImmediate.CanReachImmediate(root, targ, caster.Map, PathEndMode.Touch, null);
            }
            CellRect cellRect = (!targ.HasThing) ? CellRect.SingleCell(targ.Cell) : targ.Thing.OccupiedRect();
            float num = cellRect.ClosestDistSquaredTo(root);
            if (num > verbProps.range * verbProps.range || num < verbProps.minRange * verbProps.minRange)
            {
                resultingLine = new ShootLine(root, targ.Cell);
                return false;
            }
            //if (!this.verbProps.NeedsLineOfSight) This method doesn't consider the currently loaded projectile
            if (Projectile.projectile.flyOverhead)
            {
                resultingLine = new ShootLine(root, targ.Cell);
                return true;
            }

            // First check current cell for early opt-out
            IntVec3 dest;
            var shotSource = root.ToVector3Shifted();
            shotSource.y = ShotHeight;

            // Adjust for multi-tile turrets
            if (caster.def.building?.IsTurret ?? false)
            {
                shotSource = ShotSource;
            }

            if (CanHitFromCellIgnoringRange(shotSource, targ, out dest))
            {
                resultingLine = new ShootLine(root, dest);
                return true;
            }

            // For pawns, calculate possible lean locations
            if (CasterIsPawn)
            {
                ShootLeanUtility.LeanShootingSourcesFromTo(root, cellRect.ClosestCellTo(root), caster.Map, tempLeanShootSources);
                foreach (var leanLoc in tempLeanShootSources.OrderBy(c => c.DistanceTo(targ.Cell)))
                {
                    var leanOffset = (leanLoc - root).ToVector3() * 0.51f;  //using exactly 0.5f makes it always round up, which causes issues if offset is negative, when adding leanOffset to shotSource. Setting to 0.51f to fix.
                    if (CanHitFromCellIgnoringRange(shotSource + leanOffset, targ, out dest))
                    {
                        resultingLine = new ShootLine(leanLoc, dest);
                        return true;
                    }
                }
            }

            resultingLine = new ShootLine(root, targ.Cell);
            return false;
        }

        private bool CanHitFromCellIgnoringRange(Vector3 shotSource, LocalTargetInfo targ, out IntVec3 goodDest)
        {
            if (targ.Thing != null)
            {
                if (targ.Thing.Map != caster.Map)
                {
                    goodDest = IntVec3.Invalid;
                    return false;
                }

                if (CanHitCellFromCellIgnoringRange(shotSource, targ.Cell, targ.Thing))
                {   // if any of the locations the target is at or can lean to for shooting can be shot by the shooter then lets have the shooter shoot.
                    goodDest = targ.Cell;
                    return true;
                }
            }
            else if (CanHitCellFromCellIgnoringRange(shotSource, targ.Cell, targ.Thing))
            {
                goodDest = targ.Cell;
                return true;
            }
            goodDest = IntVec3.Invalid;
            return false;
        }

        // Added targetThing to parameters so we can calculate its height
        private bool CanHitCellFromCellIgnoringRange(Vector3 shotSource, IntVec3 targetLoc, Thing targetThing = null)
        {
            // Vanilla checks
            if (verbProps.mustCastOnOpenGround && (!targetLoc.Standable(caster.Map) || caster.Map.thingGrid.CellContains(targetLoc, ThingCategory.Pawn)))
            {
                return false;
            }
            if (verbProps.requireLineOfSight)
            {
                // Calculate shot vector
                Vector3 targetPos;
                if (targetThing != null)
                {
		    float shotHeight = shotSource.y;
		    AdjustShotHeight(caster, targetThing, ref shotHeight);
		    shotSource.y = shotHeight;
                    Vector3 targDrawPos = targetThing.DrawPos;
                    targetPos = new Vector3(targDrawPos.x, new CollisionVertical(targetThing).Max, targDrawPos.z);
                    var targPawn = targetThing as Pawn;
                    if (targPawn != null)
                    {
                        targetPos += targPawn.Drawer.leaner.LeanOffset * 0.6f;
                    }
                }
                else
                {
                    targetPos = targetLoc.ToVector3Shifted();
                }
                Ray shotLine = new Ray(shotSource, (targetPos - shotSource));

                // Create validator to check for intersection with partial cover
                var aimMode = CompFireModes?.CurrentAimMode;

                Predicate<IntVec3> CanShootThroughCell = (IntVec3 cell) =>
                {
                    Thing cover = cell.GetFirstPawn(caster.Map) ?? cell.GetCover(caster.Map);

                    if (cover != null && cover != ShooterPawn && cover != caster && cover != targetThing && !cover.IsPlant() && !(cover is Pawn && cover.HostileTo(caster)))
                    {
                        //Shooter pawns don't attempt to shoot targets partially obstructed by their own faction members or allies, except when close enough to fire over their shoulder
                        if (cover is Pawn cellPawn && !cellPawn.Downed && cellPawn.Faction != null && ShooterPawn?.Faction != null && (ShooterPawn.Faction == cellPawn.Faction || ShooterPawn.Faction.RelationKindWith(cellPawn.Faction) == FactionRelationKind.Ally) && !cellPawn.AdjacentTo8WayOrInside(ShooterPawn))
                        {
                            return false;
                        }

                        // Skip this check entirely if we're doing suppressive fire and cell is adjacent to target
                        if ((VerbPropsCE.ignorePartialLoSBlocker || aimMode == AimMode.SuppressFire) && cover.def.Fillage != FillCategory.Full) return true;

                        Bounds bounds = CE_Utility.GetBoundsFor(cover);

                        // Simplified calculations for adjacent cover for gameplay purposes
                        if (cover.def.Fillage != FillCategory.Full && cover.AdjacentTo8WayOrInside(caster))
                        {
                            // Sanity check to prevent stuff behind us blocking LoS
                            var cellTargDist = cell.DistanceTo(targetLoc);
                            var shotTargDist = shotSource.ToIntVec3().DistanceTo(targetLoc);

                            if (shotTargDist > cellTargDist)
                                return cover is Pawn || bounds.size.y < shotSource.y;
                        }

                        // Check for intersect
                        if (bounds.IntersectRay(shotLine))
                        {
                            if (Controller.settings.DebugDrawPartialLoSChecks) caster.Map.debugDrawer.FlashCell(cell, 0, bounds.extents.y.ToString());
                            return false;
                        }

                        if (Controller.settings.DebugDrawPartialLoSChecks)
                        {
                            caster.Map.debugDrawer.FlashCell(cell, 0.7f, bounds.extents.y.ToString());
                        }
                    }

                    return true;
                };

                // Add validator to parameters
                foreach (IntVec3 curCell in GenSight.PointsOnLineOfSight(shotSource.ToIntVec3(), targetLoc))
                {
                    if (Controller.settings.DebugDrawPartialLoSChecks)
                        caster.Map.debugDrawer.FlashCell(curCell, 0.4f);
                    if (curCell != shotSource.ToIntVec3() && curCell != targetLoc && !CanShootThroughCell(curCell))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        #endregion

        #endregion
    }
}
