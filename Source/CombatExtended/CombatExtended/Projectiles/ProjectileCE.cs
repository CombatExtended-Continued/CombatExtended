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
        /// <summary>
        /// Tree collision chance is multiplied by this factor
        /// </summary>
        private const float treeCollisionChance = 0.5f;
        
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
        			destinationInt = CE_Utility.GetDestination(origin, shotAngle, shotRotation, shotSpeed, shotHeight);
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
            		
            		startingTicksToImpactInt = CE_Utility.GetFlightTime(shotSpeed, shotAngle, shotHeight) * (float)GenTicks.TicksPerRealSecond;
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
		        	float seconds = Seconds;
		        	heightInt = (float)Math.Round(shotHeight + shotSpeed * Mathf.Sin(shotAngle) * seconds - (CE_Utility.gravityConst * seconds * seconds) / 2f, 3);
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
        protected int Ticks
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
        		return (ticksToImpact == 0 ? StartingTicksToImpact : (float)Ticks);
        	}
        }
        
        /// <summary>
        /// The amount of seconds multiplied by Ticks (or float StartingTicksToImpact when int ticksToImpact == 0).
        /// 
        /// This ensures that the projectile runs from 0, 1, 2, ... , n-2, n-1, n, StartingTicksToImpact with n = IntTicksToImpact = Mathf.CeilToInt(StartingTicksToImpact).
        /// </summary>
        protected float Seconds
        {
        	get
        	{
        		return fTicks / (float)GenTicks.TicksPerRealSecond;
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

        /*
         * *** End of class variables ***
        */

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
            Scribe_Values.LookValue<Vector2>(ref origin, "ori", default(Vector2), true);
            
            Scribe_Defs.LookDef(ref equipmentDef, "ed");
            Scribe_References.LookReference<Thing>(ref launcher, "lcr");
            Scribe_Values.LookValue<bool>(ref landed, "lnd", false, false);
            Scribe_Values.LookValue<int>(ref ticksToImpact, "tTI", 0, true);

            //Here be new variables
            Scribe_Values.LookValue<float>(ref shotAngle, "ang", 0f, true);
            Scribe_Values.LookValue<float>(ref shotRotation, "rot", 0f, true);
            Scribe_Values.LookValue<float>(ref shotHeight, "hgt", 0f, true);
            Scribe_Values.LookValue<float>(ref shotSpeed, "spd", 0f, true);
            Scribe_Values.LookValue<bool>(ref canTargetSelf, "cts", false, false);
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
        private bool CheckForFreeInterceptBetween(Vector2 lastExactPos, Vector2 newExactPos)
        {
            var lastPos = new IntVec3(lastExactPos);
        	var newPos = new IntVec3(newExactPos);
            if (newPos == lastPos)
            {
                return false;
            }
            if (!lastPos.InBounds(base.Map) || !newPos.InBounds(base.Map))
            {
                return false;
            }
            if ((newPos - lastPos).LengthManhattan == 1)
            {
                return CheckForFreeIntercept(newPos);
            }
            Vector2 currentExactPos = lastExactPos;
            Vector2 flightVec = newExactPos - lastExactPos;
            Vector2 sectionVec = flightVec.normalized * 0.2f;
            int numSections = (int)(flightVec.magnitude / 0.2f);
            checkedCells.Clear();
            int currentSection = 0;
            while (true)
            {
                currentExactPos += sectionVec;
                var intVec3 = new IntVec3(currentExactPos);
                if (!checkedCells.Contains(intVec3))
                {
                    if (!intVec3.InBounds(Map) || CheckForFreeIntercept(intVec3))
                    {
                        break;
                    }
                    checkedCells.Add(intVec3);
                }
                currentSection++;
                if (currentSection > numSections)
                {
                    return false;
                }
                if (intVec3 == newPos)
                {
                    return false;
                }
            }
            return true;
        }
        
        /// <summary>
        ///  Takes into account the target being downed and the projectile having been fired while the target was downed, and the target's bodySize
        /// </summary>
        /// <param name="thing">The Thing to be tested for impact at a given float height.</param>
        /// <param name="height">The height in vertical cells [vcells] to test for.</param>
        /// <returns>Whether an impact has occured (True) or not (False).</returns>
        private bool ImpactThroughBodySize(Thing thing, float height)
        {
            Pawn pawn = thing as Pawn;

            if (pawn != null)
            {
                PersonalShield shield = null;
                if (pawn.RaceProps.Humanlike)
                {
                    // check for shield user

                    List<Apparel> wornApparel = pawn.apparel.WornApparel;
                    for (int i = 0; i < wornApparel.Count; i++)
                    {
						var personalShield = wornApparel[i] as PersonalShield;
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
                    Log.Message(string.Concat("CE adding suppression: ", suppressionAmount.ToString(), " * (1 - (", compSuppressable.ParentArmor.ToString(), " - ", penetrationAmount.ToString(), ")) = ", (suppressionAmount * (1 - Mathf.Clamp(compSuppressable.ParentArmor - penetrationAmount, 0, 1))).ToString(), " to Pawn ", pawn.ToString()));
                    suppressionAmount *= 1 - Mathf.Clamp(compSuppressable.ParentArmor - penetrationAmount, 0, 1);
                    compSuppressable.AddSuppression(suppressionAmount, OriginIV3);
                }

                //Check horizontal distance
                if (CE_Utility.ClosestDistBetween(origin, Destination, new Vector2(pawn.DrawPos.x, pawn.DrawPos.z)) <= CE_Utility.GetCollisionWidth(pawn))
                {
                    //Check vertical distance
                    if (CE_Utility.GetCollisionVertical(thing).Includes(height))
                    {
                        Impact(thing);
                        return true;
                    }
                }
            }
            if (thing.def.Fillage == FillCategory.Full)
            {
                Impact(thing);
                return true;
            }
            if (thing.def.fillPercent > 0)
            {
                if (CE_Utility.GetCollisionVertical(thing).Includes(height))
                {
                    Impact(thing);
                    return true;
                }
            }
            return false;
        }

        //Unmodified
        public override void Tick()
        {
            base.Tick();
            if (landed)
            {
                return;
            }
            Vector2 exactPosition = Vec2Position;
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
                && CheckForFreeInterceptBetween(exactPosition, Vec2Position))
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

        //Added collision detection for cover objects, changed pawn collateral chances
        private bool CheckForFreeIntercept(IntVec3 cell)
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
            List<Thing> mainThingList = new List<Thing>(base.Map.thingGrid.ThingsListAtFast(cell)).Where(t => !(t is ProjectileCE) && !(t is Mote)).ToList();

            //Find pawns in adjacent cells and append them to main list
            List<IntVec3> adjList = new List<IntVec3>();
            	//HACK : Potential error
            Vector2 shotVec = Quaternion.AngleAxis(shotRotation, Vector3.up) * Vector2.down;

            //Check if bullet is going north-south or west-east
            if (Math.Abs(shotVec.x) < Math.Abs(shotVec.y))
            {
                adjList = GenAdj.CellsAdjacentCardinal(cell, Rotation, new IntVec2(0, 1)).ToList();
            }
            else
            {
                adjList = GenAdj.CellsAdjacentCardinal(cell, Rotation, new IntVec2(1, 0)).ToList();
            }

            //Iterate through adjacent cells and find all the pawns
            for (int i = 0; i < adjList.Count; i++)
            {
                if (!adjList[i].Equals(cell) && adjList[i].InBounds(base.Map))
                {
                    mainThingList.AddRange(Map.thingGrid.ThingsListAtFast(adjList[i])
                    	.Where(thing => thing.def.category == ThingCategory.Pawn));
                }
            }
            
            //Check for entries first so we avoid doing costly height calculations
            if (mainThingList.Count > 0)
            {
            	foreach (Thing thing in mainThingList.Distinct())
            	{
                    if (thing.def.Fillage == FillCategory.Full) //ignore height
                    {
                    	Impact(thing);
                        return true;
                    }
                    //Check for trees		--		HARDCODED RNG IN HERE
                    if (thing.def.category == ThingCategory.Plant && thing.def.altitudeLayer == AltitudeLayer.Building &&
                        Rand.Value <
                        thing.def.fillPercent * Mathf.Clamp(distFromOrigin / 40, 0f, 1f / treeCollisionChance) *
                        treeCollisionChance)
                    {
                        Impact(thing);
                        return true;
                    }

                    if ((launcher == null || thing != launcher || canTargetSelf) 
                        && (thing.def.category == ThingCategory.Pawn || thing.def.fillPercent > 0) 
                        && ImpactThroughBodySize(thing, Height))
                        return true;

                    /*
                    if (launcher != null && thing == launcher && !canTargetSelf)
                    {
                    	return false;
                    }

                    
                    //Checking for pawns/cover
                    if (thing.def.category == ThingCategory.Pawn)
                    {
                        // Decrease chance to hit friendly target
                        Pawn pawn = thing as Pawn;
                        if (!canTargetSelf && launcher != null && pawn.Faction != null && launcher.Faction != null && !pawn.Faction.HostileTo(launcher.Faction) && Rand.Value > 0.3)
                        {
                            return false;
                        }
                        return ImpactThroughBodySize(thing, Height);
                    }

                    //Checking for pawns/cover
                    if (thing.def.fillPercent > 0) //Need to check for fillPercent here or else will be impacting things like motes, etc.
                    {
                        return ImpactThroughBodySize(thing, Height);
                    }*/
                }
            }
            return false;
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
            // FIXME : Early opt-out
            Thing thing = Map.thingGrid.ThingAt(Position, ThingCategory.Pawn);
            if (thing != null && ImpactThroughBodySize(thing, height))
            {
                return;
            }
            List<Thing> list = Map.thingGrid.ThingsListAt(Position).Where(t => !(t is ProjectileCE) && !(t is Mote)).ToList();
            if (list.Count > 0)
            {
				foreach (var thing2 in list) {
					if (ImpactThroughBodySize(thing2, height))
						return;
				}
            }
            Impact(null);
        }

        //Unmodified
        protected virtual void Impact(Thing hitThing)
        {
            CompExplosiveCE comp = this.TryGetComp<CompExplosiveCE>();
            if (comp != null && Position.IsValid)
            {
                comp.Explode(launcher, Position, Find.VisibleMap);
            }
            Destroy();
        }
    }
}
