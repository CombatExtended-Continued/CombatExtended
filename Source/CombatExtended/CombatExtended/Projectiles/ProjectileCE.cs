using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CombatExtended
{
    public abstract class ProjectileCE : ThingWithComps
    {
        private const int SuppressionRadius = 3;    // Suppression is applied within this area
        private const int collisionCheckSize = 5;    // Check for collision with multi-cell pawns and apply suppression in area of this size, centered on flight path.
        
        protected Vector2 origin;
        
        private IntVec3 originInt = new IntVec3(0, -1000, 0);
        protected IntVec3 OriginIV3
        {
        	get
        	{
        		if (originInt.y < 0)
        		{
        			originInt = new IntVec3(origin);
        		}
        		return originInt;
        	}
        }
        
    	protected Vector3 destinationInt = new Vector3(0f, 0f, -1f);
        protected Vector2 Destination
        {
        	get
        	{
        		if (destinationInt.z < 0)
        		{
        			destinationInt = GetDestination(origin, shotAngle, shotRotation, shotSpeed, shotHeight);
        			destinationInt.z = 0f;
        		}
        		// Since returning as a Vector2 yields Vector2(Vector3.x, Vector3.y)!
        		return destinationInt;
        	}
        }
        
        protected ThingDef equipmentDef;
        protected Thing launcher;
        public float minCollisionSqr;
        public bool canTargetSelf;
        
        #region Vanilla
        protected bool landed;
        protected int ticksToImpact;
        private Sustainer ambientSustainer;
        #endregion
        
        private float suppressionAmount;
        
        #region FreeIntercept
        private static List<IntVec3> checkedCells = new List<IntVec3>();
        #endregion
        
        float startingTicksToImpactInt = -1f;
        protected float StartingTicksToImpact
        {
            get
            {
            	if (startingTicksToImpactInt < 0f)
            	{
            		// Optimization in case shotHeight is zero (for example for fragments)
            		if (shotHeight < 0.001f)
            		{
            			// Opt-out in case the projectile is to collide instantly
            			if (shotAngle < 0f)
            			{
            				destinationInt = origin;
            				startingTicksToImpactInt = 0f;
            				ImpactSomething();
            				return 0f;
            			}
        				// Multiplied by ticksPerSecond since the calculated time is actually in seconds.
        				startingTicksToImpactInt = (float)((origin - Destination).magnitude / (Mathf.Cos(shotAngle) * shotSpeed)) * (float)GenTicks.TicksPerRealSecond;
        				return startingTicksToImpactInt;
            		}
            		
            		startingTicksToImpactInt = GetFlightTime() * (float)GenTicks.TicksPerRealSecond;
            	}
                return startingTicksToImpactInt;
            }
        }
        
        #region Height
        private bool heightOutdated = true;
        private float heightInt = 0f;
        /// <summary>
        /// If heightOutdated is true, Height calculates the quadratic formula (g/2)t^2 + (-v_0y)t + (y-y0) for {g -> gravity, v_0y -> shotSpeed * Mathf.Sin(shotAngle), y0 -> shotHeight, t -> seconds} to find y rounded to the nearest 3 decimals.
        /// 
        /// If heightOutdated is false, it returns a locally stored value heightInt which is the product of previous calculation.
        /// </summary>
        public float Height
        {
        	get
        	{
        		if (heightOutdated)
        		{
		        	heightInt = GetHeightAtTicks(FlightTicks);
		        	heightOutdated = false;
        		}
        		return heightInt;
        	}
        }
        #endregion
        
        #region Ticks/Seconds
        /// <summary>
        /// An integer ceil value of StartingTicksToImpact. Is equal to -1 when not initialized.
        /// </summary>
        int intTicksToImpact = -1;
        protected int IntTicksToImpact
        {
        	get
        	{
        		if (intTicksToImpact < 0)
        		{
        			intTicksToImpact = Mathf.CeilToInt(StartingTicksToImpact);
        		}
        		return intTicksToImpact;
        	}
        }
        
        /// <summary>
        /// The amount of ticks this projectile has remained in the air for.
        /// </summary>
        protected int FlightTicks
        {
        	get
        	{
        		return IntTicksToImpact - ticksToImpact;
        	}
        }
        protected float fTicks
        {
        	get
        	{
        		return (ticksToImpact == 0 ? StartingTicksToImpact : (float)FlightTicks);
        	}
        }
        #endregion

        #region Position
        private Vector2 Vec2Position
        {
        	get
        	{
        		return Vector2.Lerp(origin, Destination, fTicks / StartingTicksToImpact);
        	}
        }
        
        public virtual Vector3 ExactPosition
        {
            get
            {
            	var v = Vec2Position;
            	return new Vector3(v.x, 0f, v.y);
            }
        }

        public virtual Quaternion ExactRotation
        {
            get
            {
            	return Quaternion.AngleAxis(shotRotation, Vector3.up);
            }
        }

        public override Vector3 DrawPos
        {
            get
            {
            	var v = Vec2Position;
            	return new Vector3(v.x, def.Altitude, v.y);
            	//return new Vector3(v.x, def.Altitude, v.y + 0.5f*Height);
            }
        }
        #endregion

        /// <summary>
        /// Angle off the ground [radians].
        /// </summary>
        public float shotAngle = 0f;
        /// <summary>
        /// Angle rotation between shooter and destination [degrees].
        /// </summary>
        public float shotRotation = 0f;
        /// <summary>
        /// Shot height in vertical cells. Humans start their shot at 0.85f [vcells].
        /// </summary>
        public float shotHeight = 0f;
        /// <summary>
        /// The assigned shot speed, in general equal to the projectile.def.speed value.
        /// </summary>
        public float shotSpeed = -1f;

        private float _gravityFactor = -1;

        private float GravityFactor
        {
            get
            {
                if (_gravityFactor < 0)
                {
                    _gravityFactor = CE_Utility.gravityConst;
                    var props = def.projectile as ProjectilePropertiesCE;
                    if (props != null) _gravityFactor = props.Gravity;
                }
                return _gravityFactor;
            }
        }

        /*
         * *** End of class variables ***
        */

        #region Methods

        /// <summary>
        /// Saves new variables shotAngle, shotHeight, shotSpeed.
        /// </summary>
        public override void ExposeData()
        {
        	base.ExposeData();
            
            if (Scribe.mode == LoadSaveMode.Saving && launcher != null && launcher.Destroyed)
            {
                launcher = null;
            }
            Scribe_Values.Look(ref origin, "ori", default(Vector2), true);
            
            Scribe_Defs.Look(ref equipmentDef, "ed");
            Scribe_References.Look(ref launcher, "lcr");
            Scribe_Values.Look(ref landed, "lnd", false, false);
            Scribe_Values.Look(ref ticksToImpact, "tTI", 0, true);

            //Here be new variables
            Scribe_Values.Look(ref shotAngle, "ang", 0f, true);
            Scribe_Values.Look(ref shotRotation, "rot", 0f, true);
            Scribe_Values.Look(ref shotHeight, "hgt", 0f, true);
            Scribe_Values.Look(ref shotSpeed, "spd", 0f, true);
            Scribe_Values.Look(ref canTargetSelf, "cts", false, false);
        }

        /// <summary>
        /// Physics-enabled Launch() method.
        /// </summary>
        /// <param name="launcher">The Thing that launched this projectile.</param>
        /// <param name="origin">The origin of the projectile (different from the launcher for e.g grenade fragments)</param>
        /// <param name="shotAngle">Angle off the ground [radians].</param>
        /// <param name="shotRotation">Rotation between shooter and destination [degrees].</param>
        /// <param name="shotHeight">The shot height, usually the max height of the non-pawn caster, a portion of the height of the pawn caster OR zero. (default: 0)</param>
        /// <param name="shotSpeed">The shot speed (default: def.projectile.speed)</param>
        /// <param name="equipment">The equipment used to fire the projectile.</param>
        public virtual void Launch(Thing launcher, Vector2 origin, float shotAngle, float shotRotation, float shotHeight = 0f, float shotSpeed = -1f, Thing equipment = null)
        {
            this.shotAngle = shotAngle;
            this.shotHeight = shotHeight;
            this.shotRotation = shotRotation;
            
        	Launch(launcher, origin, equipment);
            if (shotSpeed > 0f)
            {
                this.shotSpeed = shotSpeed;
            }
            
            ticksToImpact = IntTicksToImpact;
        }
        
        public virtual void Launch(Thing launcher, Vector2 origin, Thing equipment = null)
        {
        	this.shotSpeed = def.projectile.speed;
            this.launcher = launcher;
            this.origin = origin;
            equipmentDef = (equipment != null) ? equipment.def : null;
            
            if (!def.projectile.soundAmbient.NullOrUndefined())
            {
                SoundInfo info = SoundInfo.InMap(this, MaintenanceType.PerTick);
                ambientSustainer = def.projectile.soundAmbient.TrySpawnSustainer(info);
            }
        }

        //Removed minimum collision distance
        private bool CheckForCollisionBetween(Vector2 lastExactPos, Vector2 newExactPos)
        {
            var lastPos = new IntVec3(lastExactPos);
        	var newPos = new IntVec3(newExactPos);
            // Sanity checks
            if (newPos == lastPos)
            {
                return false;
            }
            if (!lastPos.InBounds(base.Map) || !newPos.InBounds(base.Map))
            {
                return false;
            }

            if (DebugViewSettings.drawInterceptChecks)
            {
                Map.debugDrawer.FlashLine(lastPos, newPos);
            }

            // Determine flight path
            var from = new Vector3(lastExactPos.x, GetHeightAtTicks(FlightTicks - 1), lastExactPos.y);
            var dest = new Vector3(newExactPos.x, GetHeightAtTicks(FlightTicks), newExactPos.y);
            var shotLine = new Ray(from, (dest - from));

            // Early opt-out, if we only moved by one cell only check the new cell
            /*
            if ((newPos - lastPos).LengthManhattan == 1)
            {
                if (DebugViewSettings.drawInterceptChecks)
                {
                    Map.debugDrawer.FlashCell(newPos, 1, "1");
                }
                return CheckCellForCollision(newPos, shotLine);
            }
            */

            // Iterate through all cells between the last and the new position
            var cells = GenSight.PointsOnLineOfSight(lastPos, newPos);
            foreach(IntVec3 cell in cells)
            {
                if (CheckCellForCollision(cell, shotLine))
                {
                    return true;
                }

                if (DebugViewSettings.drawInterceptChecks)
                {
                    Map.debugDrawer.FlashCell(cell, 1, "o");
                }
            }
            return false;
        }

        /// <summary>
        /// Checks whether a collision occurs along flight path within this cell.
        /// </summary>
        /// <param name="cell">Where to check for collisions in</param>
        /// <param name="shotLine">Projectile's flight path</param>
        /// <returns>True if collision occured, false otherwise</returns>
        private bool CheckCellForCollision(IntVec3 cell, Ray shotLine)
        {
            //Check for minimum collision distance
            float distFromOrigin = (cell - OriginIV3).LengthHorizontalSquared;
            if (!def.projectile.alwaysFreeIntercept
                && minCollisionSqr <= 1f
                ? distFromOrigin < 1f
                : distFromOrigin < Mathf.Min(144f, minCollisionSqr / 4))
            {
                return false;
            }
            List<Thing> mainThingList = new List<Thing>(base.Map.thingGrid.ThingsListAtFast(cell)).Where(t => t is Pawn || t.def.Fillage != FillCategory.None).ToList();

            //Find pawns in adjacent cells and append them to main list
            List<IntVec3> adjList = new List<IntVec3>();
            adjList.AddRange(GenAdj.CellsAdjacentCardinal(cell, Rot4.FromAngleFlat(shotRotation), new IntVec2(collisionCheckSize, 0)).ToList());

            //Iterate through adjacent cells and find all the pawns
            foreach (IntVec3 curCell in adjList)
            {
                if (curCell != cell && curCell.InBounds(base.Map))
                {
                    mainThingList.AddRange(Map.thingGrid.ThingsListAtFast(curCell)
                        .Where(thing => thing.def.category == ThingCategory.Pawn));

                    if (DebugViewSettings.drawInterceptChecks)
                    {
                        Map.debugDrawer.FlashCell(curCell, 0.7f);
                    }
                }
            }

            foreach (Thing thing in mainThingList.Distinct())
            {
                if (thing == launcher && !canTargetSelf) continue;

                // Skip collision detection for walls and such
                if (thing.def.Fillage == FillCategory.Full)
                {
                    Impact(thing);
                    return true;
                }

                // Apply suppression
                Pawn pawn = thing as Pawn;
                if (pawn != null)
                {
                    ApplySuppression(pawn);
                }

                // Check for collision
                if (TryCollideWith(thing, shotLine)) return true;
            }
            return false;
        }

        /// <summary>
        /// Tries to impact the thing based on whether it intersects the given flight path. Trees have RNG chance to not collide even on intersection. 
        /// </summary>
        /// <param name="thing">What to impact</param>
        /// <param name="shotLine">Projectile's path of travel</param>
        /// <returns>True if impact occured, false otherwise</returns>
        private bool TryCollideWith(Thing thing, Ray shotLine)
        {
            if (thing == launcher && !canTargetSelf)
            {
                return false;
            }

            // Trees have RNG chance to collide
            if (thing.IsTree())
            {
                float chance = thing.def.fillPercent * ((thing.Position - OriginIV3).LengthHorizontal / 40);
                if (Controller.settings.DebugShowTreeCollisionChance) MoteMaker.ThrowText(thing.Position.ToVector3Shifted(), thing.Map, chance.ToString());
                if (!Rand.Chance(chance)) return false;
            }
            else
            {
                var bounds = CE_Utility.GetBoundsFor(thing);
                if (!bounds.IntersectRay(shotLine))
                {
                    return false;
                }
            }


            if (DebugViewSettings.drawInterceptChecks) MoteMaker.ThrowText(thing.Position.ToVector3Shifted(), thing.Map, "x", Color.red);
            Impact(thing);
            return true;
        }

        private void ApplySuppression(Pawn pawn)
        {
            ShieldBelt shield = null;
            if (pawn.RaceProps.Humanlike)
            {
                // check for shield user

                List<Apparel> wornApparel = pawn.apparel.WornApparel;
                for (int i = 0; i < wornApparel.Count; i++)
                {
                    var personalShield = wornApparel[i] as ShieldBelt;
                    if (personalShield != null)
                    {
                        shield = personalShield;
                        break;
                    }
                }
            }
            //Add suppression
            CompSuppressable compSuppressable = pawn.TryGetComp<CompSuppressable>();
            if (compSuppressable != null
                && pawn.Faction != launcher?.Faction
                && (shield == null || shield?.ShieldState == ShieldState.Resetting))
            {
                suppressionAmount = def.projectile.damageAmountBase;
                ProjectilePropertiesCE propsCE = def.projectile as ProjectilePropertiesCE;
                float penetrationAmount = propsCE == null ? 0f : propsCE.armorPenetration;
                suppressionAmount *= 1 - Mathf.Clamp(compSuppressable.ParentArmor - penetrationAmount, 0, 1);
                compSuppressable.AddSuppression(suppressionAmount, OriginIV3);
            }
        }

        //Unmodified
        public override void Tick()
        {
            base.Tick();
            if (landed)
            {
                return;
            }
            Vector2 lastExactPosition = Vec2Position;
            ticksToImpact--;
            if (!ExactPosition.InBounds(base.Map))
            {
                ticksToImpact++;
                Position = ExactPosition.ToIntVec3();
                Destroy(DestroyMode.Vanish);
                return;
            }
            heightOutdated = true;
            if (ticksToImpact >= 0
                && !def.projectile.flyOverhead
                && CheckForCollisionBetween(lastExactPosition, Vec2Position))
            {
                return;
            }
            Position = ExactPosition.ToIntVec3();
            if (ticksToImpact == 60 && Find.TickManager.CurTimeSpeed == TimeSpeed.Normal &&
                def.projectile.soundImpactAnticipate != null)
            {
                def.projectile.soundImpactAnticipate.PlayOneShot(this);
            }
            if (ticksToImpact <= 0)
            {
                ImpactSomething();
                return;
            }
            if (ambientSustainer != null)
            {
                ambientSustainer.Maintain();
            }
        }

        //Unmodified
        public override void Draw()
        {
            Graphics.DrawMesh(MeshPool.plane10, DrawPos, ExactRotation, def.DrawMatSingle, 0);
            Comps_PostDraw();
        }

        //Modified collision with downed pawns
        private void ImpactSomething()
        {
            //Not modified, just mortar code
            if (def.projectile.flyOverhead)
            {
                RoofDef roofDef = base.Map.roofGrid.RoofAt(Position);
                if (roofDef != null)
                {
                    if (roofDef.isThickRoof)
                    {
                        this.def.projectile.soundHitThickRoof.PlayOneShot(new TargetInfo(base.Position, base.Map, false));
                        this.Destroy(DestroyMode.Vanish);
                        return;
                    }
                    if (base.Position.GetEdifice(base.Map) == null || base.Position.GetEdifice(base.Map).def.Fillage != FillCategory.Full)
                    {
                        RoofCollapserImmediate.DropRoofInCells(base.Position, base.Map);
                    }
                }
            }
            //Modified
            var height = Height;

            // Determine flight path - Need to refactor this to be less hacky
            var pos = Vec2Position;
            var lastPos = Vector2.Lerp(this.origin, Destination, (fTicks - 1) / StartingTicksToImpact);

            var vec3dest = new Vector3(pos.x, GetHeightAtTicks(FlightTicks), pos.y);
            var vec3lastPos = new Vector3(lastPos.x, GetHeightAtTicks(FlightTicks - 1), lastPos.y);
            var shotLine = new Ray(vec3lastPos, (vec3dest - vec3lastPos));

            // FIXME : Early opt-out
            Thing thing = Position.GetFirstPawn(Map);
            if (thing != null && TryCollideWith(thing, shotLine))
            {
                return;
            }
            List<Thing> list = Map.thingGrid.ThingsListAt(Position).Where(t => t is Pawn || t.def.Fillage != FillCategory.None).ToList();
            if (list.Count > 0)
            {
				foreach (var thing2 in list) {
					if (TryCollideWith(thing2, shotLine))
						return;
				}
            }
            Impact(null);
        }
        
        protected virtual void Impact(Thing hitThing)
        {
            CompExplosiveCE comp = this.TryGetComp<CompExplosiveCE>();
            if (comp != null && Position.IsValid)
            {
                comp.Explode(launcher, Position, Find.VisibleMap);
            }

            // Apply suppression around impact area
            var suppressThings = GenRadial.RadialDistinctThingsAround(Position, Map, SuppressionRadius + def.projectile.explosionRadius, true);
            foreach (Thing thing in suppressThings)
            {
                Pawn pawn = thing as Pawn;
                if (pawn != null) ApplySuppression(pawn);
            }

            Destroy();
        }

        private float GetHeightAtTicks(int ticks)
        {
            float seconds = ((float)ticks) / GenTicks.TicksPerRealSecond;
            return (float)Math.Round(shotHeight + shotSpeed * Mathf.Sin(shotAngle) * seconds - (GravityFactor * seconds * seconds) / 2f, 3);
        }

        #region Ballistics
        /// <summary>
        /// Calculates the time in seconds the arc characterized by <i>angle</i>, <i>shotHeight</i> takes to traverse at speed <i>velocity</i> - e.g until the height reaches zero. Does not take into account air resistance.
        /// </summary>
        /// <param name="velocity">Projectile velocity in cells per second.</param>
        /// <param name="angle">Shot angle in radians off the ground.</param>
        /// <param name="shotHeight">Height from which the projectile is fired in vertical cells.</param>
        /// <returns>Time in seconds that the projectile will take to traverse the given arc.</returns>
        private float GetFlightTime()
        {
            //Calculates quadratic formula (g/2)t^2 + (-v_0y)t + (y-y0) for {g -> gravity, v_0y -> vSin, y -> 0, y0 -> shotHeight} to find t in fractional ticks where height equals zero.
            return (Mathf.Sin(shotAngle) * shotSpeed + Mathf.Sqrt(Mathf.Pow(Mathf.Sin(shotAngle) * shotSpeed, 2f) + 2f * GravityFactor * shotHeight)) / GravityFactor;
        }

        /// <summary>
        /// Calculates the range reachable with a projectile of speed <i>velocity</i> fired at <i>angle</i> from height <i>shotHeight</i>. Does not take into account air resistance.
        /// </summary>
        /// <param name="velocity">Projectile velocity in cells per second.</param>
        /// <param name="angle">Shot angle in radians off the ground.</param>
        /// <param name="shotHeight">Height from which the projectile is fired in vertical cells.</param>
        /// <returns>Distance in cells that the projectile will fly at the given arc.</returns>
        private float GetDistanceTraveled(float velocity, float angle, float shotHeight)
        {
            if (shotHeight < 0.001f)
            {
                return (Mathf.Pow(velocity, 2f) / GravityFactor) * Mathf.Sin(2f * angle);
            }
            return ((velocity * Mathf.Cos(angle)) / GravityFactor) * (velocity * Mathf.Sin(angle) + Mathf.Sqrt(Mathf.Pow(velocity * Mathf.Sin(angle), 2f) + 2f * GravityFactor * shotHeight));
        }

        /// <summary>
        /// Calculates the destination reached with a projectile of speed <i>velocity</i> fired at <i>angle</i> from height <i>shotHeight</i> starting from <i>origin</i>. Does not take into account air resistance.
        /// </summary>
        /// <param name="origin">Vector2 source of the projectile.</param>
        /// <param name="angle">Shot angle in radians off the ground.</param>
        /// <param name="rotation">Shot angle in degrees between source/target.</param>
        /// <param name="velocity">Projectile velocity in cells per second.</param>
        /// <param name="shotHeight">Height from which the projectile is fired in vertical cells.</param>
        /// <returns>The Vector2 destination of the projectile, e.g the Vector2 when it hits the ground at height = 0f.</returns>
        private Vector2 GetDestination(Vector2 origin, float angle, float rotation, float velocity, float shotHeight)
        {
            return origin + Vector2.up.RotatedBy(rotation) * GetDistanceTraveled(velocity, angle, shotHeight);
        }

        /// <summary>
        /// Calculates the shot angle necessary to reach <i>range</i> with a projectile of speed <i>velocity</i> at a height difference of <i>heightDifference</i>, returning either the upper or lower arc in radians. Does not take into account air resistance.
        /// </summary>
        /// <param name="velocity">Projectile velocity in cells per second.</param>
        /// <param name="range">Cells between shooter and target.</param>
        /// <param name="heightDifference">Difference between initial shot height and target height in vertical cells.</param>
        /// <param name="flyOverhead">Whether to take the lower (False) or upper (True) arc angle.</param>
        /// <returns>Arc angle in radians off the ground.</returns>
        public static float GetShotAngle(float velocity, float range, float heightDifference, bool flyOverhead, float gravity)
        {
            return Mathf.Atan((Mathf.Pow(velocity, 2f) + (flyOverhead ? 1f : -1f) * Mathf.Sqrt(Mathf.Pow(velocity, 4f) - gravity * (gravity * Mathf.Pow(range, 2f) + 2f * heightDifference * Mathf.Pow(velocity, 2f)))) / (gravity * range));
        }
        #endregion

        #endregion
    }
}
