using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CombatExtended
{
	[StaticConstructorOnStartup]
    public abstract class ProjectileCE : ThingWithComps
    {
    	private static readonly Material ShadowMat = MaterialPool.MatFrom("Things/Projectile/BulletShadow", ShaderDatabase.Transparent);
    	
    	#region ClassVariables
    	/// <summary>
    	/// Suppression is applied within this radius (x-y and z)
    	/// </summary>
        private const int SuppressionRadius = 3;
        
        /// <summary>
        /// Check for collision with multi-cell pawns and apply suppression in radius of this size, centered on flight path.
        /// </summary>
        private const int collisionCheckSize = 5;
        
        #region Origin destination
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
        #endregion
        
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
        
        #region Height
        private int lastHeightTick = -1;
        private float heightInt = 0f;
        /// <summary>
        /// If lastHeightTick is not FlightTicks, Height calculates the quadratic formula (g/2)t^2 + (-v_0y)t + (y-y0) for {g -> gravity, v_0y -> shotSpeed * Mathf.Sin(shotAngle), y0 -> shotHeight, t -> seconds} to find y rounded to the nearest 3 decimals.
        /// 
        /// If lastHeightTick equals FlightTicks, it returns a locally stored value heightInt which is the product of previous calculation.
        /// </summary>
        public float Height
        {
        	get
        	{
        		if (lastHeightTick != FlightTicks)
        		{
        			heightInt = ticksToImpact > 0 ? GetHeightAtTicks(FlightTicks) : 0f;
		        	lastHeightTick = FlightTicks;
        		}
        		return heightInt;
        	}
        }
        #endregion
        
        #region Ticks/Seconds
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
        
        int intTicksToImpact = -1;
        /// <summary>
        /// An integer ceil value of StartingTicksToImpact. intTicksToImpact is equal to -1 when not initialized.
        /// </summary>
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
        /// The amount of integer ticks this projectile has remained in the air for, ignoring impact.
        /// </summary>
        protected int FlightTicks
        {
        	get
        	{
        		return IntTicksToImpact - ticksToImpact;
        	}
        }
        /// <summary>
        /// The amount of float ticks the projectile has remained in the air for, including impact.
        /// </summary>
        protected float fTicks
        {
        	get
        	{
        		return (ticksToImpact == 0 ? StartingTicksToImpact : (float)FlightTicks);
        	}
        }
        #endregion

        #region Position
        private Vector2 Vec2Position(float ticks = -1f)
        {
        	if (ticks < 0)
        	{
        		ticks = fTicks;
        	}
        	return Vector2.Lerp(origin, Destination, ticks / StartingTicksToImpact);
        }
        
        private Vector3 impactPosition = new Vector3();
        /// <summary>
        /// Exact x,y,z (x,height,y) position in terms of Vec2Position.x, .y (lerped origin to Destination) and Height.
        /// </summary>
        public virtual Vector3 ExactPosition
        {
        	set
        	{
        		impactPosition = new Vector3(value.x, value.y, value.z);
        		Position = impactPosition.ToIntVec3();
        	}
            get
            {
            	if (landed)
            	{
            		return impactPosition;
            	}
	        	var v = Vec2Position();
	        	return new Vector3(v.x, Height, v.y);
            }
        }
        
        public Vector2 DrawPosV2
        {
        	get
        	{
        		return Vec2Position() + new Vector2(0, Height - shotHeight * ((StartingTicksToImpact - fTicks) / StartingTicksToImpact));
        	}
        }
        
        public override Vector3 DrawPos
        {
            get
            {
            	var v = DrawPosV2;
            	return new Vector3(v.x, def.Altitude, v.y);
            }
        }
        
        private Vector3 lastExactPos = new Vector3(-1000, 0, 0);
        private Vector3 LastPos
        {
        	set
        	{
        		lastExactPos = value;
        	}
        	get
        	{
        		if (lastExactPos.x <-999)
        		{
	        		var lastPos = Vec2Position(FlightTicks - 1);
	        		lastExactPos = new Vector3(lastPos.x, GetHeightAtTicks(FlightTicks - 1), lastPos.y);
        		}
        		return lastExactPos;
        	}
        }
        
        private int lastShotLine = -1;
        private Ray shotLine;
        public Ray ShotLine
        {
        	get
        	{
        		if (lastShotLine != FlightTicks)
        		{
		            shotLine = new Ray(LastPos, (ExactPosition - LastPos));
		            lastShotLine = FlightTicks;
        		}
        		return shotLine;
        	}
        }
        #endregion
        
        #region Angle
        private float drawnShotAngle = 1000f;
        /// <summary>
        /// Drawn shot angle [degrees] for input in the Draw() method. Takes into account the displayed arc corrections.
        /// </summary>
        public float DrawnShotAngleCorrection
        {
        	get
        	{
        		if (drawnShotAngle > 999f)
        		{
        			drawnShotAngle = Mathf.Rad2Deg*(Mathf.Sin(Mathf.Deg2Rad * shotRotation)*(shotAngle + Mathf.Atan2(shotHeight, GetDistanceTraveled(shotSpeed, shotAngle, shotHeight))));
        		}
        		return drawnShotAngle;
        	}
        }
        
        public Quaternion DrawRotation
        {
            get
            {
            	return Quaternion.AngleAxis(shotRotation + Mathf.Lerp(-DrawnShotAngleCorrection, DrawnShotAngleCorrection, fTicks / StartingTicksToImpact) , Vector3.up);
            }
        }
        
        public virtual Quaternion ExactRotation
        {
            get
            {
            	return Quaternion.AngleAxis(shotRotation, Vector3.up);
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
        /// The assigned shot speed (not speed in z axis or x-y plane), in general equal to the projectile.def.speed value.
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
		#endregion
        
        /*
         * *** End of class variables ***
        */

        #region Methods

        #region Expose
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
        #endregion
		
        #region Launch
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
            	//For explosives/bullets, equipmentDef is important
            equipmentDef = (equipment != null) ? equipment.def : null;
            
            if (!def.projectile.soundAmbient.NullOrUndefined())
            {
                SoundInfo info = SoundInfo.InMap(this, MaintenanceType.PerTick);
                ambientSustainer = def.projectile.soundAmbient.TrySpawnSustainer(info);
            }
        }
        #endregion
		
        #region Collisions
        //Removed minimum collision distance
        private bool CheckForCollisionBetween()
        {
        	var lastPosIV3 = LastPos.ToIntVec3();
        	var newPosIV3 = ExactPosition.ToIntVec3();
        	
            // Sanity checks
            if (newPosIV3 == lastPosIV3)
            {
                return false;
            }
            if (!lastPosIV3.InBounds(base.Map) || !newPosIV3.InBounds(base.Map))
            {
                return false;
            }

            if (DebugViewSettings.drawInterceptChecks)
            {
                Map.debugDrawer.FlashLine(lastPosIV3, newPosIV3);
            }

            /* Early opt-out, if we only moved by one cell only check the new cell
            if ((newPos - lastPos).LengthManhattan == 1)
            {
                if (DebugViewSettings.drawInterceptChecks)
                {
                    Map.debugDrawer.FlashCell(newPos, 1, "1");
                }
                return CheckCellForCollision(newPos, shotLine);
            }
            */

           	/*
        	//Set to Vector3.left (-1,0,0) as a sort of Vector3.Invalid (namely x < 0)
        	Vector3 roofCollision = Vector3.left;
        	
        	//If there's a sign change, e.g intersection of height with WallCollisionHeight
        	if (Mathf.Sign(LastPos.y - CollisionVertical.WallCollisionHeight)
        	    != Mathf.Sign(ExactPosition.y - CollisionVertical.WallCollisionHeight))
        	{
        		float enter;
        		//Find the exact intersection coordinates
        		CollisionVertical.RoofCollisionPlane.Raycast(ShotLine, out enter);
        		//Check if the tile is roofed
        		roofCollision = ShotLine.GetPoint(enter);
        	}*/
        	
            // Iterate through all cells between the last and the new position
            var cells = GenSight.PointsOnLineOfSight(lastPosIV3, newPosIV3).OrderBy(x => (x.ToVector3Shifted() - LastPos).MagnitudeHorizontalSquared());
			
    		//Order cells by distance from the last position
            foreach(IntVec3 cell in cells)
            {
            	if (CheckCellForCollision(cell))
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
        /// <returns>True if collision occured, false otherwise</returns>
        private bool CheckCellForCollision(IntVec3 cell)
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
            List<Thing> mainThingList = new List<Thing>(base.Map.thingGrid.ThingsListAtFast(cell))
            	.Where(t => t is Pawn || t.def.Fillage != FillCategory.None).ToList();

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
			
            //If the last position is above the wallCollisionHeight, we should check for roof intersections first
            if (LastPos.y > CollisionVertical.WallCollisionHeight
               && TryCollideWithRoof(cell))
            	return true;
            
            foreach (Thing thing in mainThingList.Distinct().OrderBy(x => (x.DrawPos - LastPos).sqrMagnitude))
            {
                if (thing == launcher && !canTargetSelf) continue;

                /* Skip collision detection for walls and such
                if (thing.def.Fillage == FillCategory.Full)
                {
                    Impact(thing);
                    return true;
                }*/

                // Apply suppression. The height here is NOT that of the bullet in CELL,
                // it is the height at the END OF THE PATH. This is because SuppressionRadius
                // is not considered an EXACT limit.
                if (ExactPosition.y < SuppressionRadius)
                {
	                Pawn pawn = thing as Pawn;
	                if (pawn != null)
	                {
	                    ApplySuppression(pawn);
	                }
                }
				
                // Check for collision
                if (TryCollideWith(thing)) return true;
            }
            
            //Finally check for intersecting with (a) roof.
			return TryCollideWithRoof(cell);
        }

        private bool TryCollideWithRoof(IntVec3 cell)
        {
        	if (!cell.Roofed(Map)) return false;
        	
        	var bounds = CE_Utility.GetBoundsFor(cell, cell.GetRoof(Map));
            float dist;
            if (!bounds.IntersectRay(ShotLine, out dist))
            {
                return false;
            }
            
            var point = ShotLine.GetPoint(dist);
            ExactPosition = point;
        	landed = true;
        	
            if (DebugViewSettings.drawInterceptChecks) MoteMaker.ThrowText(cell.ToVector3Shifted(), Map, "x", Color.red);
            
            Impact(null);
            return true;
        }
        
        /// <summary>
        /// Tries to impact the thing based on whether it intersects the given flight path. Trees have RNG chance to not collide even on intersection. 
        /// </summary>
        /// <param name="thing">What to impact</param>
        /// <returns>True if impact occured, false otherwise</returns>
        private bool TryCollideWith(Thing thing)
        {
            if (thing == launcher && !canTargetSelf)
            {
                return false;
            }

            var bounds = CE_Utility.GetBoundsFor(thing);
            float dist;
            if (!bounds.IntersectRay(ShotLine, out dist))
            {
                return false;
            }
            
            // Trees and bushes have RNG chance to collide
			var plant = thing as Plant;
            if (plant != null)
            {
            	//TODO: Remove fillPercent dependency because all fillPercents on trees are 0.25
            	//Prevents trees near the shooter (e.g the shooter's cover) to be hit
                float chance = thing.def.fillPercent * ((thing.Position - OriginIV3).LengthHorizontal / 40);
                if (Controller.settings.DebugShowTreeCollisionChance) MoteMaker.ThrowText(thing.Position.ToVector3Shifted(), thing.Map, chance.ToString());
                if (!Rand.Chance(chance)) return false;
            }
            
            var point = ShotLine.GetPoint(dist);
            if (!point.InBounds(this.Map))
            	Log.Error("TryCollideWith out of bounds point from ShotLine: obj " + thing.ThingID + ", proj " + this.ThingID + ", dist " + dist + ", point " + point);
            	
            ExactPosition = point;
        	landed = true;
        	
            if (DebugViewSettings.drawInterceptChecks) MoteMaker.ThrowText(thing.Position.ToVector3Shifted(), thing.Map, "x", Color.red);
            
            Impact(thing);
            return true;
        }
        #endregion

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

        public override void Tick()
        {
            base.Tick();
            if (landed)
            {
                return;
            }
            LastPos = ExactPosition;
            ticksToImpact--;
            if (!ExactPosition.InBounds(base.Map))
            {
                Position = LastPos.ToIntVec3();
                Destroy(DestroyMode.Vanish);
                return;
            }
            if (ticksToImpact >= 0
                && !def.projectile.flyOverhead
                && CheckForCollisionBetween())
            {
                return;
            }
            Position = ExactPosition.ToIntVec3();
            if (ticksToImpact == 60 && Find.TickManager.CurTimeSpeed == TimeSpeed.Normal &&
                def.projectile.soundImpactAnticipate != null)
            {
                def.projectile.soundImpactAnticipate.PlayOneShot(this);
            }
            	//TODO : It appears that the final step in the arc doesn't CheckForCollisionBetween.
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
        
        /// <summary>
        /// Draws projectile if at least a tick away from caster (or always if no caster)
        /// </summary>
        public override void Draw()
        {
        	if (FlightTicks == 0 && launcher != null)
        	{
        		//Draw at the end of the barrel on the pawn
        	}
        	else
        	{
	        	Graphics.DrawMesh(MeshPool.plane10, DrawPos, DrawRotation, def.DrawMatSingle, 0);
	            
	            //Add edifice height
	            var shadowPos = new Vector3(ExactPosition.x,
	                                        def.Altitude - 0.01f,
	                                        ExactPosition.z - Mathf.Lerp(shotHeight, 0f, fTicks / StartingTicksToImpact));
	                                        		//EXPERIMENTAL: + (new CollisionVertical(ExactPosition.ToIntVec3().GetEdifice(Map))).Max);
	            
	            //Vary ShadowMat plane by projectile size/type/damage or something
	            Graphics.DrawMesh(MeshPool.plane08, shadowPos, ExactRotation, ShadowMat, 0);
	            
	            Comps_PostDraw();
        	}
        }

        #region Impact
        //Modified collision with downed pawns
        private void ImpactSomething()
        {
            var pos = ExactPosition.ToIntVec3();
            
            //Not modified, just mortar code
            if (def.projectile.flyOverhead)
            {
            	RoofDef roofDef = base.Map.roofGrid.RoofAt(pos);
                if (roofDef != null)
                {
                    if (roofDef.isThickRoof)
                    {
                        this.def.projectile.soundHitThickRoof.PlayOneShot(new TargetInfo(pos, base.Map, false));
                        this.Destroy(DestroyMode.Vanish);
                        return;
                    }
                    if (pos.GetEdifice(base.Map) == null || pos.GetEdifice(base.Map).def.Fillage != FillCategory.Full)
                    {
                        RoofCollapserImmediate.DropRoofInCells(pos, base.Map);
                    }
                }
            }

            // FIXME : Early opt-out
            Thing thing = pos.GetFirstPawn(Map);
            if (thing != null && TryCollideWith(thing))
            {
                return;
            }
            List<Thing> list = Map.thingGrid.ThingsListAt(pos).Where(t => t is Pawn || t.def.Fillage != FillCategory.None).ToList();
            if (list.Count > 0)
            {
				foreach (var thing2 in list) {
					if (TryCollideWith(thing2))
						return;
				}
            }
            Impact(null);
        }
        
        protected virtual void Impact(Thing hitThing)
        {
            CompExplosiveCE comp = this.TryGetComp<CompExplosiveCE>();
            if (comp != null && ExactPosition.ToIntVec3().IsValid)
            {
                comp.Explode(launcher, ExactPosition.ToIntVec3(), Find.VisibleMap);
            }
			
            // Opt-out for things without explosionRadius
            if (def.projectile.explosionRadius > 0 && ExactPosition.y < SuppressionRadius)
            {
            	// Apply suppression around impact area
	            var suppressThings = GenRadial.RadialDistinctThingsAround(ExactPosition.ToIntVec3(), Map, SuppressionRadius + def.projectile.explosionRadius, true);
	            foreach (Thing thing in suppressThings)
	            {
	                Pawn pawn = thing as Pawn;
	                if (pawn != null) ApplySuppression(pawn);
	            }
            }

            Destroy();
        }
        
        private void ImpactRoof(IntVec3 cell)
        {
        	
        }
		#endregion

        #region Ballistics
		/// <summary>
		/// Calculated rounding to three decimales the output of h0 + v * sin(a0) * t - g/2 * t^2 with {h0 -> shotHeight, v -> shotSpeed, a0 -> shotAngle, t -> ticks/GenTicks.TicksPerRealSecond, g -> GravityFactor}. Called roughly each tick for impact checks and for drawing.
		/// </summary>
		/// <param name="ticks">Integer ticks, since the only time value which is not an integer (accessed by StartingTicksToImpact) has height zero by definition.</param>
		/// <returns>Projectile height at time ticks in ticks.</returns>
        private float GetHeightAtTicks(int ticks)
        {
            float seconds = ((float)ticks) / GenTicks.TicksPerRealSecond;
            return (float)Math.Round(shotHeight + shotSpeed * Mathf.Sin(shotAngle) * seconds - (GravityFactor * seconds * seconds) / 2f, 3);
        }
        
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
