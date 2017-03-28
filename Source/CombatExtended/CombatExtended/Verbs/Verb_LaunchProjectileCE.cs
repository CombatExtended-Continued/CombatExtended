using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class Verb_LaunchProjectileCE : Verse.Verb
    {
        #region Constants
        
        // Cover check constants
        private const float distToCheckForCover = 3f;   // How many cells to raycast on the cover check
        private const float segmentLength = 0.2f;       // How long a single raycast segment is
        private const float shotHeightFactor = 0.85f;   // The height at which pawns hold their guns

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
        private float shotSpeed = -1;
        private float shotHeight = -1;

        private int lastTauntTick;
        
        #endregion

        #region Properties

        public VerbPropertiesCE VerbPropsCE => this.verbProps as VerbPropertiesCE;
        public ProjectilePropertiesCE projectilePropsCE => this.ProjectileDef.projectile as ProjectilePropertiesCE;

        // Returns either the pawn aiming the weapon or in case of turret guns the turret operator or null if neither exists
        public Pawn ShooterPawn => CasterPawn == null ? CasterPawn : CE_Utility.TryGetTurretOperator(this.caster);

        protected CompCharges CompCharges
        {
            get
            {
                if (this.compCharges == null && this.ownerEquipment != null)
                {
                    this.compCharges = this.ownerEquipment.TryGetComp<CompCharges>();
                }
                return this.compCharges;
            }
        }
        private float ShotSpeed
        {
            get
            {
                if (shotSpeed < 0)
                {
                    if (CompCharges != null)
                    {
                        Vector2 bracket;
                        if (CompCharges.GetChargeBracket((currentTarget.Cell - caster.Position).LengthHorizontal, out bracket))
                        {
                            shotSpeed = bracket.x;
                        }
                    }
                    else
                    {
                        shotSpeed = verbProps.projectileDef.projectile.speed;
                    }
                }
                return shotSpeed;
            }
        }
        private float ShotHeight
        {
            get
            {
                if (shotHeight < 0)
                {
                	var shooterVertical = CE_Utility.GetCollisionVertical(caster);
                	shotHeight = CasterIsPawn
                		? shooterVertical.min + (shooterVertical.max - shooterVertical.min) * shotHeightFactor
		            	: shooterVertical.max;
                }
                return shotHeight;
            }
        }

        protected float ShootingAccuracy => CasterPawn?.GetStatValue(StatDefOf.ShootingAccuracy) ?? 2f;
        protected float AimingAccuracy => ShooterPawn?.GetStatValue(CE_StatDefOf.AimingAccuracy) ?? 0.75f;
        protected float SightsEfficiency => (3 - ownerEquipment.GetStatValue(CE_StatDefOf.SightsEfficiency));
        protected virtual float SwayAmplitude => (4.5f - ShootingAccuracy) * this.ownerEquipment.GetStatValue(StatDef.Named("SwayFactor"));

        // Ammo variables
        protected CompAmmoUser CompAmmo
        {
            get
            {
                if (compAmmo == null && this.ownerEquipment != null)
                {
                    compAmmo = this.ownerEquipment.TryGetComp<CompAmmoUser>();
                }
                return compAmmo;
            }
        }
        public ThingDef ProjectileDef
        {
            get
            {
                if (CompAmmo != null)
                {
                    if (CompAmmo.currentAmmo != null)
                    {
                        return CompAmmo.CurAmmoProjectile;
                    }
                }
                return this.VerbPropsCE.projectileDef;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Highlights explosion radius of the projectile if it has one
        /// </summary>
        /// <returns>Projectile explosion radius</returns>
        public override float HighlightFieldRadiusAroundTarget()
        {
            return ProjectileDef.projectile.explosionRadius;
        }

        /// <summary>
        /// Resets current burst shot count and estimated distance at beginning of the burst
        /// </summary>
        public override void WarmupComplete()
        {
            // attack shooting expression
            Pawn shooter = ShooterPawn;
            if (ModSettings.showTaunts 
                && shooter != null 
                && shooter.Map != null 
                && shooter.def.race.Humanlike 
                && currentTarget != null
                && currentTarget.Thing is Pawn
                && Find.TickManager.TicksGame - lastTauntTick >= 120
                && Rand.Chance(0.25f))
            {
                string rndswear = RulePackDef.Named("AttackMote").Rules.RandomElement().Generate();
                if (rndswear == "[swear]" || rndswear == "" || rndswear == " ")
                {
                    Log.Warning("CE tried throwing invalid taunt for " + shooter.ToString());
                }
                else
                {
                    MoteMaker.ThrowText(shooter.Position.ToVector3Shifted(), shooter.Map, rndswear);
                }
                lastTauntTick = Find.TickManager.TicksGame;
            }

            this.numShotsFired = 0;
            base.WarmupComplete();
        }

        float rotationDegrees = 0f;
        float angleRadians = 0f;
        
        /// <summary>
        /// Shifts the original target position in accordance with target leading, range estimation and weather/lighting effects
        /// </summary>
        protected virtual void ShiftTarget(ShiftVecReport report, bool calculateMechanicalOnly = false)
        {
        	if (!calculateMechanicalOnly)
        	{
	        	Vector3 u = CasterPawn != null ? CasterPawn.DrawPos : caster.Position.ToVector3Shifted();
	        	sourceLoc.Set(u.x, u.z);
	        	
        		if (this.numShotsFired == 0)
        		{
	            	// On first shot of burst do a range estimate
        			estimatedTargDist = report.GetRandDist();
        		}
        	
	            Vector3 v = report.targetPawn != null ? report.targetPawn.DrawPos : report.target.Cell.ToVector3Shifted();
	            newTargetLoc.Set(v.x, v.z);
	            
	            // ----------------------------------- STEP 1: Actual location + Shift for visibility
	
	            	//FIXME : GetRandCircularVec may be causing recoil to be unnoticeable - each next shot in the burst has a new random circular vector around the target.
	            newTargetLoc += report.GetRandCircularVec();
	
	            // ----------------------------------- STEP 2: Estimated shot to hit location
	
	            newTargetLoc = sourceLoc + (newTargetLoc - sourceLoc).normalized * estimatedTargDist;
	
	            // Lead a moving target
	            newTargetLoc += report.GetRandLeadVec();
	
	            // ----------------------------------- STEP 3: Recoil, Skewing, Skill checks, Cover calculations
				
	            rotationDegrees = 0f;
	            angleRadians = 0f;
	            
	            GetSwayVec(ref rotationDegrees, ref angleRadians);
	            GetRecoilVec(ref rotationDegrees, ref angleRadians);
	
			    // Height difference calculations for ShotAngle
			    float targetHeight = 0f;
	            
	            var coverVertical = CE_Utility.GetCollisionVertical(report.cover, true);	//Get " " cover, assume it is the edifice
	            
	            // Projectiles with flyOverhead target the ground below the target and ignore cover
	            if (ProjectileDef.projectile.flyOverhead)
	            {
	            	targetHeight = coverVertical.max;
	            }
	            else
	            {
	           		var targetVertical = CE_Utility.GetCollisionVertical(currentTarget.Thing);	//Get lower and upper heights of the target
	           		if (targetVertical.min < coverVertical.max)	//Some part of the target is hidden behind cover
	           		{
	           			//TODO : It is possible for targetVertical.max < coverVertical.max, technically, in which case the shooter will never hit until the cover is gone.
	           			targetVertical.min = coverVertical.max;
	           		}
                    /*
                    else if (currentTarget.Thing is Pawn)
                    {
                        // Aim for center of mass on an exposed target
                        targetVertical.min += CE_Utility.bodyRegionBottomHeight * targetVertical.max;
                        targetVertical.max *= CE_Utility.bodyRegionMiddleHeight;
                    }
                    */
	           		targetHeight = targetVertical.min + (targetVertical.max - targetVertical.min) * 0.5f;
	            }
	            
	            angleRadians += CE_Utility.GetShotAngle(ShotSpeed, (newTargetLoc - sourceLoc).magnitude, targetHeight - ShotHeight, ProjectileDef.projectile.flyOverhead);
        	}
        	
	        // ----------------------------------- STEP 4: Mechanical variation
	        
            // Get shotvariation, in angle Vector2 RADIANS.
            Vector2 spreadVec = report.GetRandSpreadVec();
            
            // ----------------------------------- STEP 5: Finalization
            
            var w = (newTargetLoc - sourceLoc);
            shotRotation = (90 + Mathf.Rad2Deg * Mathf.Atan2(-w.y, w.x) + rotationDegrees + spreadVec.x) % 360;
            shotAngle = angleRadians + spreadVec.y * Mathf.Deg2Rad;
        }

        /// <summary>
        /// Calculates the amount of recoil at a given point in a burst, up to a maximum
        /// </summary>
        /// <param name="rotation">The ref float to have horizontal recoil in degrees added to.</param>
        /// <param name="angle">The ref float to have vertical recoil in radians added to.</param>
        private void GetRecoilVec(ref float rotation, ref float angle)
        {
            float minX = 0;
            float maxX = 0;
            float minY = 0;
            float maxY = 0;
            switch (VerbPropsCE.recoilPattern)
            {
                case RecoilPattern.None:
            		return;
                case RecoilPattern.Regular:
                    float num = VerbPropsCE.recoilAmount / 3;
                    minX = -(num / 3);
                    maxX = num;
                    minY = -num;
                    maxY = VerbPropsCE.recoilAmount;
                    break;
                case RecoilPattern.Mounted:
                    float num2 = VerbPropsCE.recoilAmount / 3;
                    minX = -num2;
                    maxX = num2;
                    minY = -num2;
                    maxX = VerbPropsCE.recoilAmount;
                    break;
            }
            float recoilMagnitude = Mathf.Pow((5 - ShootingAccuracy), (Mathf.Min(10, numShotsFired) / 6.25f));
            
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
        	float ticks = (float)(Find.TickManager.TicksAbs + this.caster.thingIDNumber);
        	rotation += SwayAmplitude * (float)Mathf.Sin(ticks * 0.022f);
        	angle += Mathf.Deg2Rad * 0.25f * SwayAmplitude * (float)Mathf.Sin(ticks * 0.0165f);
        }

        public virtual ShiftVecReport ShiftVecReportFor(LocalTargetInfo target)
        {
            IntVec3 targetCell = target.Cell;
            ShiftVecReport report = new ShiftVecReport();
            report.target = target;
            report.aimingAccuracy = this.AimingAccuracy;
            report.sightsEfficiency = this.SightsEfficiency;
            report.shotDist = (targetCell - this.caster.Position).LengthHorizontal;

            report.lightingShift = 1 - caster.Map.glowGrid.GameGlowAt(targetCell);
            if (!this.caster.Position.Roofed(caster.Map) || !targetCell.Roofed(caster.Map))  //Change to more accurate algorithm?
            {
                report.weatherShift = 1 - caster.Map.weatherManager.CurWeatherAccuracyMultiplier;
            }
            report.shotSpeed = this.ShotSpeed;
            report.swayDegrees = this.SwayAmplitude;
            report.spreadDegrees = this.ownerEquipment.GetStatValue(StatDef.Named("ShotSpread")) * this.projectilePropsCE.spreadMult;
            Thing cover;
            this.GetPartialCoverBetween(this.caster.Position.ToVector3Shifted(), targetCell.ToVector3Shifted(), out cover);
            report.cover = cover;

            return report;
        }

        /// <summary>
        /// Checks for cover along the flight path of the bullet, doesn't check for walls or plants, only intended for cover with partial fillPercent
        /// </summary>
        /// <param name="sourceLoc">The position from which to start checking</param>
        /// <param name="targetLoc">The position of the target</param>
        /// <param name="cover">Output parameter, filled with the highest cover object found</param>
        /// <returns>True if cover was found, false otherwise</returns>
        private bool GetPartialCoverBetween(Vector3 sourceLoc, Vector3 targetLoc, out Thing cover)
        {
            sourceLoc.Scale(new Vector3(1, 0, 1));
            targetLoc.Scale(new Vector3(1, 0, 1));

            //Calculate segment vector and segment amount
            Vector3 shotVec = sourceLoc - targetLoc;    //Vector from target to source
            Vector3 segmentVec = shotVec.normalized * segmentLength;
            float distToCheck = Mathf.Min(distToCheckForCover, shotVec.magnitude);  //The distance to raycast
            float numSegments = distToCheck / segmentLength;

            //Raycast accross all segments to check for cover
            List<IntVec3> checkedCells = new List<IntVec3>();
            Thing thingAtTargetLoc = targetLoc.ToIntVec3().GetEdifice(caster.Map);
            Thing newCover = null;
            for (int i = 0; i <= numSegments; i++)
            {
                IntVec3 cell = (targetLoc + segmentVec * i).ToIntVec3();
                if (!checkedCells.Contains(cell))
                {
                    //Cover check, if cell has cover compare fillPercent and get the highest piece of cover, ignore if cover is the target (e.g. solar panels, crashed ship, etc)
                    Thing coverAtCell = GridsUtility.GetCover(cell, caster.Map);
                    if (coverAtCell != null
                        && (thingAtTargetLoc == null || !coverAtCell.Equals(thingAtTargetLoc))
                        && (newCover == null || newCover.def.fillPercent < coverAtCell.def.fillPercent)
                        && coverAtCell.def.Fillage != FillCategory.Full
                        && coverAtCell.def.category != ThingCategory.Plant)
                    {
                        newCover = coverAtCell;
                    }
                }
            }
            cover = newCover;

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
            if (!targ.Cell.InBounds(caster.Map) || !root.InBounds(caster.Map)) return false;

            //Sanity check for flyOverhead projectiles, they should not attack things under thick roofs
            if (ProjectileDef.projectile.flyOverhead)
            {
                RoofDef roofDef = caster.Map.roofGrid.RoofAt(targ.Cell);
                if (roofDef != null && roofDef.isThickRoof)
                {
                    return false;
                }
                return base.CanHitTargetFrom(root, targ);
            }
            if (base.CanHitTargetFrom(root, targ))
            {
                //Check if target is obstructed behind cover
                Thing coverTarg;
                if (GetPartialCoverBetween(root.ToVector3Shifted(), targ.Cell.ToVector3Shifted(), out coverTarg))
                {
                    if (CE_Utility.GetCollisionVertical(targ.Thing).max < CE_Utility.GetCollisionVertical(coverTarg, true).max)
                    {
                        return false;
                    }
                }
                //Check if shooter is obstructed by cover
                Thing coverShoot;
                if (GetPartialCoverBetween(targ.Cell.ToVector3Shifted(), root.ToVector3Shifted(), out coverShoot))
                {
                	if (ShotHeight < CE_Utility.GetCollisionVertical(coverShoot, true).max)
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Fires a projectile using the new aiming system
        /// </summary>
        /// <returns>True for successful shot, false otherwise</returns>
        protected override bool TryCastShot()
        {
            ShootLine shootLine;
            if (!TryFindShootLineFromTo(caster.Position, currentTarget, out shootLine))
            {
                return false;
            }
            if (projectilePropsCE.pelletCount < 1)
            {
                Log.Error(ownerEquipment.LabelCap + " tried firing with pelletCount less than 1.");
                return false;
            }
            ShiftVecReport report = ShiftVecReportFor(currentTarget);
           	bool pelletMechanicsOnly = false;
            for (int i = 0; i < projectilePropsCE.pelletCount; i++)
            {
                ProjectileCE projectile = (ProjectileCE)ThingMaker.MakeThing(ProjectileDef, null);
                GenSpawn.Spawn(projectile, shootLine.Source, caster.Map);
	           	//Vector3 targetVec3 = ShiftTarget(report, pelletMechanicsOnly);
	           	ShiftTarget(report, pelletMechanicsOnly);

                //New aiming algorithm
                projectile.canTargetSelf = verbProps.targetParams.canTargetSelf;
                projectile.minCollisionSqr = (sourceLoc - newTargetLoc).sqrMagnitude;
                projectile.Launch(caster, sourceLoc, shotAngle, shotRotation, ShotHeight, ShotSpeed, ownerEquipment);
                
                /*projectile.shotAngle = this.shotAngle;
                projectile.shotHeight = this.shotHeight;
                projectile.shotSpeed = this.shotSpeed;
                if (this.currentTarget.Thing != null)
                {
                    projectile.Launch(this.caster, casterExactPosition, new LocalTargetInfo(this.currentTarget.Thing), targetVec3, this.ownerEquipment);
                }
                else
                {
                    projectile.Launch(this.caster, casterExactPosition, new LocalTargetInfo(shootLine.Dest), targetVec3, this.ownerEquipment);
                }*/
	           	pelletMechanicsOnly = true;
            }
           	pelletMechanicsOnly = false;
            this.numShotsFired++;
            return true;
        }

        /// <summary>
        /// This is a custom CE ticker. Since the vanilla VerbTick() method is non-virtual we need to detour VerbTracker and make it call this method in addition to the vanilla ticker in order to
        /// add custom ticker functionality.
        /// </summary>
        public virtual void VerbTickCE()
        {
        }

        #endregion
    }
}
