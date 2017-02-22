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
        protected Vector3 origin;
        protected Vector3 destination;
        protected Thing assignedTarget;
        public bool canFreeIntercept;
        protected ThingDef equipmentDef;
        protected Thing launcher;
        private Thing assignedMissTargetInt;
        protected bool landed;
        private float suppressionAmount;
        protected int ticksToImpact;
        private Sustainer ambientSustainer;
        private static List<IntVec3> checkedCells = new List<IntVec3>();
        public static readonly String[] robotBodyList = { "AIRobot", "HumanoidTerminator" };
        
        private const int ticksPerSecond = 100;

        public Thing AssignedMissTarget
        {
            get { return assignedMissTargetInt; }
            set
            {
                if (value.def.Fillage == FillCategory.Full)
                {
                    return;
                }
                assignedMissTargetInt = value;
            }
        }

        float startingTicksToImpactInt = -1f;
        protected float StartingTicksToImpact
        {
            get
            {
            	if (startingTicksToImpactInt < 0f)
            	{
            		//Optimization in case shotHeight is zero (for example for fragments)
            		if (shotHeight < 0.001f)
            		{
            			//Multiplied by ticksPerSecond since the calculated time is actually in seconds.
            			startingTicksToImpactInt = (float)((origin - destination).magnitude / (Mathf.Cos(shotAngle) * shotSpeed)) * (float)ticksPerSecond;
            		}
            		else
            		{
            			//Stored to decrease the amount of Math.Sin calls.
            			const float gravity = CE_Utility.gravityConst;
	            		float vSin = (float)(Math.Sin(shotAngle) * shotSpeed);
	            		
            			//Calculates quadratic formula (g/2)t^2 + (-v_0y)t + (y-y0) for {g -> gravity, v_0y -> vSin, y -> 0, y0 -> shotHeight} to find t in fractional ticks where height equals zero.
	            		startingTicksToImpactInt = (float)((vSin + Mathf.Sqrt(Mathf.Pow(vSin, 2) + 2 * gravity * shotHeight)) / gravity) * (float)ticksPerSecond;
            		}
            	}
                return startingTicksToImpactInt;
            }
        }

        protected IntVec3 DestinationCell
        {
            get
            {
                return new IntVec3(destination);
            }
        }

        public virtual Vector3 ExactPosition
        {
            get
            {
            	Vector3 b = (destination - origin) * (1f - (float)((float)ticksToImpact / StartingTicksToImpact));
                return origin + b + Vector3.up * def.Altitude;
            }
        }

        public virtual Quaternion ExactRotation
        {
            get
            {
                return Quaternion.LookRotation(destination - origin);
            }
        }

        public override Vector3 DrawPos
        {
            get
            {
                return ExactPosition;
            }
        }

        //New variables
        private const float treeCollisionChance = 0.5f; //Tree collision chance is multiplied by this factor
        /// <summary>
        /// Angle in radians
        /// </summary>
        public float shotAngle;
        public float shotHeight = 0f;
        public float shotSpeed = -1f;

        private float distanceFromOrigin
        {
            get
            {
                Vector3 currentPos = Vector3.Scale(ExactPosition, new Vector3(1, 0, 1));
                return (currentPos - origin).magnitude;
            }
        }

        /*
         * *** End of class variables ***
        */

        //Keep track of new variables
        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving && launcher != null && launcher.Destroyed)
            {
                launcher = null;
            }
            Scribe_Values.LookValue(ref origin, "origin", default(Vector3), false);
            Scribe_Values.LookValue(ref destination, "destination", default(Vector3), false);
            Scribe_References.LookReference(ref assignedTarget, "assignedTarget");
            Scribe_Values.LookValue(ref canFreeIntercept, "canFreeIntercept", false, false);
            Scribe_Defs.LookDef(ref equipmentDef, "equipmentDef");
            Scribe_References.LookReference(ref launcher, "launcher");
            Scribe_References.LookReference(ref assignedMissTargetInt, "assignedMissTarget");
            Scribe_Values.LookValue(ref landed, "landed", false, false);
            Scribe_Values.LookValue(ref ticksToImpact, "ticksToImpact", 0, false);

            //Here be new variables
            Scribe_Values.LookValue(ref shotAngle, "shotAngle", 0f, true);
            Scribe_Values.LookValue(ref shotHeight, "shotHeight", 0f, true);
            Scribe_Values.LookValue(ref shotSpeed, "shotSpeed", 0f, true);
        }

        public float GetProjectileHeight()
        {
        	const float gravity = CE_Utility.gravityConst;
        	float seconds = (StartingTicksToImpact - (float)ticksToImpact) / (float)ticksPerSecond;
            //Calculates quadratic formula (g/2)t^2 + (-v_0y)t + (y-y0) for {g -> gravity, v_0y -> shotSpeed * Mathf.Sin(shotAngle), y0 -> shotHeight, t -> seconds} to find y rounded to the nearest 3 decimals.
        	float height = (float)Math.Round(shotHeight + shotSpeed * Mathf.Sin(shotAngle) * seconds - (gravity * seconds * seconds) / 2f, 3);
        	return height;
        }

        //Added new method, takes Vector3 destination as argument
        public void Launch(Thing launcher, Vector3 origin, LocalTargetInfo targ, Vector3 target, Thing equipment = null)
        {
            destination = target;
            Launch(launcher, origin, targ, equipment);
        }

        //Added new calculations for downed pawns, destination
        public virtual void Launch(Thing launcher, Vector3 origin, LocalTargetInfo targ, Thing equipment = null)
        {
            if (shotSpeed < 0)
            {
                shotSpeed = def.projectile.speed;
            }
            this.launcher = launcher;
            this.origin = origin;
            if (equipment != null)
            {
                equipmentDef = equipment.def;
            }
            else
            {
                equipmentDef = null;
            }
            //Checking if target was downed on launch
            if (targ.Thing != null)
            {
                assignedTarget = targ.Thing;
            }
            //Checking if a new destination was set
            if (destination == null)
            {
                destination = targ.Cell.ToVector3Shifted() +
                              new Vector3(Rand.Range(-0.3f, 0.3f), 0f, Rand.Range(-0.3f, 0.3f));
            }

            ticksToImpact = Mathf.CeilToInt(StartingTicksToImpact);
            if (!def.projectile.soundAmbient.NullOrUndefined())
            {
                SoundInfo info = SoundInfo.InMap(this, MaintenanceType.PerTick);
                ambientSustainer = def.projectile.soundAmbient.TrySpawnSustainer(info);
            }
        }

        //Removed minimum collision distance
        private bool CheckForFreeInterceptBetween(Vector3 lastExactPos, Vector3 newExactPos)
        {
            IntVec3 lastPos = lastExactPos.ToIntVec3();
            IntVec3 newPos = newExactPos.ToIntVec3();
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
            //Check for minimum collision distance
            float distToTarget = assignedTarget != null
                ? (assignedTarget.DrawPos - origin).MagnitudeHorizontal()
                : (destination - origin).MagnitudeHorizontal();
            if (def.projectile.alwaysFreeIntercept
                || distToTarget <= 1f
                ? origin.ToIntVec3().DistanceToSquared(newPos) > 1f
                : origin.ToIntVec3().DistanceToSquared(newPos) > Mathf.Min(12f, distToTarget / 2))
            {
                Vector3 currentExactPos = lastExactPos;
                Vector3 flightVec = newExactPos - lastExactPos;
                Vector3 sectionVec = flightVec.normalized * 0.2f;
                int numSections = (int)(flightVec.MagnitudeHorizontal() / 0.2f);
                checkedCells.Clear();
                int currentSection = 0;
                while (true)
                {
                    currentExactPos += sectionVec;
                    IntVec3 intVec3 = currentExactPos.ToIntVec3();
                    if (!checkedCells.Contains(intVec3))
                    {
                        if (CheckForFreeIntercept(intVec3))
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
            return false;
        }

        /// <summary>
        ///     Takes into account the target being downed and the projectile having been fired while the target was downed, and
        ///     the target's bodySize
        /// </summary>
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
                        if (wornApparel[i] is PersonalShield)
                        {
                            shield = (PersonalShield)wornApparel[i];
                            break;
                        }
                    }
                }
                //Add suppression
                CompSuppressable compSuppressable = pawn.TryGetComp<CompSuppressable>();
                if (compSuppressable != null)
                {
                    if (shield == null || (shield != null && shield?.ShieldState == ShieldState.Resetting))
                    {
                        /*
                        if (pawn.skills.GetSkill(SkillDefOf.Shooting).level >= 1)
                        {
                            suppressionAmount = (def.projectile.damageAmountBase * (1f - ((pawn.skills.GetSkill(SkillDefOf.Shooting).level) / 100) * 3));
                        }
                        else suppressionAmount = def.projectile.damageAmountBase;
                        */
                        suppressionAmount = def.projectile.damageAmountBase;
                        ProjectilePropertiesCE propsCE = def.projectile as ProjectilePropertiesCE;
                        float penetrationAmount = propsCE == null ? 0f : propsCE.armorPenetration;
                        suppressionAmount *= 1 - Mathf.Clamp(compSuppressable.parentArmor - penetrationAmount, 0, 1);
                        compSuppressable.AddSuppression(suppressionAmount, origin.ToIntVec3());
                    }
                }

                //Check horizontal distance
                Vector3 dest = destination;
                Vector3 orig = origin;
                Vector3 pawnPos = pawn.DrawPos;
                float closestDistToPawn = Math.Abs((dest.z - orig.z) * pawnPos.x - (dest.x - orig.x) * pawnPos.z +
                                                 dest.x * orig.z - dest.z * orig.x)
                                        /
                                        (float)
                                            Math.Sqrt((dest.z - orig.z) * (dest.z - orig.z) +
                                                      (dest.x - orig.x) * (dest.x - orig.x));
                if (closestDistToPawn <= CE_Utility.GetCollisionWidth(pawn))
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
        public void Launch(Thing launcher, LocalTargetInfo targ, Thing equipment = null)
        {
            Launch(launcher, Position.ToVector3Shifted(), targ, equipment);
        }

        //Unmodified
        public override void Tick()
        {
            base.Tick();
            if (landed)
            {
                return;
            }
            Vector3 exactPosition = ExactPosition;
            ticksToImpact--;
            if (!ExactPosition.InBounds(base.Map))
            {
                ticksToImpact++;
                Position = ExactPosition.ToIntVec3();
                Destroy(DestroyMode.Vanish);
                return;
            }
            Vector3 exactPosition2 = ExactPosition;
            if (ticksToImpact >= 0 && !def.projectile.flyOverhead && canFreeIntercept
                && CheckForFreeInterceptBetween(exactPosition, exactPosition2))
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
                if (DestinationCell.InBounds(base.Map))
                {
                    Position = DestinationCell;
                }
                ImpactSomething();
                return;
            }
            if (ambientSustainer != null)
            {
                ambientSustainer.Maintain();
            }
            // attack shooting expression
            if (ModSettings.showTaunts && this.launcher is Building_TurretGunCE == false)
            {
                if (Rand.Value > 0.7
                    && this.launcher.def.race.Humanlike
                    && !robotBodyList.Contains(this.launcher.def.race.body.defName)
                    && Gen.IsHashIntervalTick(launcher, Rand.Range(280, 700)))
                {
                    AGAIN: string rndswear = RulePackDef.Named("AttackMote").Rules.RandomElement().Generate();
                    if (rndswear == "[swear]" || rndswear == "" || rndswear == " ")
                    {
                        goto AGAIN;
                    }
                    MoteMaker.ThrowText(launcher.Position.ToVector3Shifted(), this.launcher.Map, rndswear);
                }
            }
        }

        //Added collision detection for cover objects, changed pawn collateral chances
        private bool CheckForFreeIntercept(IntVec3 cell)
        {
            //Check for minimum collision distance
            float distFromOrigin = (cell.ToVector3Shifted() - origin).MagnitudeHorizontal();
            float distToTarget = assignedTarget != null
                ? (assignedTarget.DrawPos - origin).MagnitudeHorizontal()
                : (destination - origin).MagnitudeHorizontal();
            if (!def.projectile.alwaysFreeIntercept
                && distToTarget <= 1f
                ? distFromOrigin < 1f
                : distFromOrigin < Mathf.Min(12f, distToTarget / 2))
            {
                return false;
            }
            List<Thing> mainThingList = new List<Thing>(base.Map.thingGrid.ThingsListAt(cell));

            //Find pawns in adjacent cells and append them to main list
            List<IntVec3> adjList = new List<IntVec3>();
            Vector3 shotVec = (destination - origin).normalized;

            //Check if bullet is going north-south or west-east
            if (Math.Abs(shotVec.x) < Math.Abs(shotVec.z))
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
                if (adjList[i].InBounds(base.Map) && !adjList[i].Equals(cell))
                {
                    List<Thing> thingList = new List<Thing>(base.Map.thingGrid.ThingsListAt(adjList[i]));
                    List<Thing> pawns =
                        thingList.Where(
                            thing => thing.def.category == ThingCategory.Pawn && !mainThingList.Contains(thing))
                            .ToList();
                    mainThingList.AddRange(pawns);
                }
            }

            //Check for entries first so we avoid doing costly height calculations
            if (mainThingList.Count > 0)
            {
            	float height = GetProjectileHeight();
                for (int i = 0; i < mainThingList.Count; i++)
                {
                    Thing thing = mainThingList[i];
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

                    //Checking for pawns/cover
                    if (thing.def.category == ThingCategory.Pawn)
                    {
                        // Decrease chance to hit friendly target
                        Pawn pawn = thing as Pawn;
                        if (this.launcher != null && pawn.Faction != null && this.launcher.Faction != null && !pawn.Faction.HostileTo(this.launcher.Faction) && Rand.Value > 0.3)
                        {
                            return false;
                        }
                        return ImpactThroughBodySize(thing, height);
                    }

                    //Checking for pawns/cover
                    if (ticksToImpact < StartingTicksToImpact / 2 && thing.def.fillPercent > 0) //Need to check for fillPercent here or else will be impacting things like motes, etc.
                    {
                        return ImpactThroughBodySize(thing, height);
                    }
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
                RoofDef roofDef = base.Map.roofGrid.RoofAt(base.Position);
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
            if (assignedTarget != null && assignedTarget.Position == Position && ImpactThroughBodySize(assignedTarget, GetProjectileHeight()))
            //it was aimed at something and that something is still there
            {
            	return;
            }
            Thing thing = base.Map.thingGrid.ThingAt(Position, ThingCategory.Pawn);
            if (thing != null && ImpactThroughBodySize(thing, GetProjectileHeight()))
            {
                return;
            }
            List<Thing> list = base.Map.thingGrid.ThingsListAt(Position);
            if (list.Count > 0)
            {
            	var height = GetProjectileHeight();
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
            if (comp != null && launcher != null && this.Position.IsValid)
            {
                comp.Explode(launcher, Position, Find.VisibleMap);
            }
            Destroy(DestroyMode.Vanish);
        }

        //Unmodified
        public void ForceInstantImpact()
        {
            if (!this.DestinationCell.InBounds(base.Map))
            {
                Destroy(DestroyMode.Vanish);
                return;
            }
            this.ticksToImpact = 0;
            base.Position = DestinationCell;
            this.ImpactSomething();
        }
    }
}