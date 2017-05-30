using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;

namespace CombatExtended
{
    static class CE_Utility
    {
        #region Misc

        public static List<ThingDef> allWeaponDefs = new List<ThingDef>();

        /// <summary>
        /// Generates a random Vector2 in a circle with given radius
        /// </summary>
        public static Vector2 GenRandInCircle(float radius)
        {
            //Fancy math to get random point in circle
            System.Random rand = new System.Random();
            double angle = rand.NextDouble() * Math.PI * 2;
            double range = Math.Sqrt(rand.NextDouble()) * radius;
            return new Vector2((float)(range * Math.Cos(angle)), (float)(range * Math.Sin(angle)));
        }

        /// <summary>
        /// Calculates the actual current movement speed of a pawn
        /// </summary>
        /// <param name="pawn">Pawn to calculate speed of</param>
        /// <returns>Move speed in cells per second</returns>
        public static float GetMoveSpeed(Pawn pawn)
        {
            float movePerTick = 60 / pawn.GetStatValue(StatDefOf.MoveSpeed, false);    //Movement per tick
            movePerTick +=  pawn.Map.pathGrid.CalculatedCostAt(pawn.Position, false, pawn.Position);
            Building edifice = pawn.Position.GetEdifice(pawn.Map);
            if (edifice != null)
            {
                movePerTick += (int)edifice.PathWalkCostFor(pawn);
            }

            //Case switch to handle walking, jogging, etc.
            if (pawn.CurJob != null)
            {
                switch (pawn.CurJob.locomotionUrgency)
                {
                    case LocomotionUrgency.Amble:
                        movePerTick *= 3;
                        if (movePerTick < 60)
                        {
                            movePerTick = 60;
                        }
                        break;
                    case LocomotionUrgency.Walk:
                        movePerTick *= 2;
                        if (movePerTick < 50)
                        {
                            movePerTick = 50;
                        }
                        break;
                    case LocomotionUrgency.Jog:
                        break;
                    case LocomotionUrgency.Sprint:
                        movePerTick = Mathf.RoundToInt(movePerTick * 0.75f);
                        break;
                }
            }
            return 60 / movePerTick;
        }
        
        public static float ClosestDistBetween(Vector2 origin, Vector2 destination, Vector2 target)
        {
        	return Mathf.Abs((destination.y - origin.y) * target.x - (destination.x - origin.x) * target.y + destination.x * origin.y - destination.y * origin.x) / (destination - origin).magnitude;
        }

        /// <summary>
        /// Attempts to find a turret operator. Accepts any Thing as input and does a sanity check to make sure it is an actual turret.
        /// </summary>
        /// <param name="thing">The turret to check for an operator</param>
        /// <returns>Turret operator if one is found, null if not</returns>
        public static Pawn TryGetTurretOperator(Thing thing)
        {
            Pawn manningPawn = null;
            Building_TurretGun turret = thing as Building_TurretGun;
            if (turret != null)
            {
                CompMannable comp = turret.TryGetComp<CompMannable>();
                if (comp != null && comp.MannedNow)
                {
                    manningPawn = comp.ManningPawn;
                }
            }
            return manningPawn;
        }

        /// <summary>
        /// Extension method to determine whether a ranged weapon has ammo available to it
        /// </summary>
        /// <returns>True if the gun has no CompAmmoUser, doesn't use ammo or has ammo in its magazine or carrier inventory, false otherwise</returns>
        public static bool HasAmmo(this ThingWithComps gun)
        {
            CompAmmoUser comp = gun.TryGetComp<CompAmmoUser>();
            if (comp == null) return true;
            return !comp.useAmmo || comp.curMagCount > 0 || comp.hasAmmo;
        }

        public static bool CanBeStabilizied(this Hediff diff)
        {
            HediffWithComps hediff = diff as HediffWithComps;
            if (hediff == null)
            {
                return false;
            }
            if (hediff.BleedRate == 0 || hediff.IsTended() || hediff.IsOld())
            {
                return false;
            }
            HediffComp_Stabilize comp = hediff.TryGetComp<HediffComp_Stabilize>();
            return comp != null && !comp.Stabilized;
        }

        #endregion Misc

        #region MoteThrower
        public static void ThrowEmptyCasing(Vector3 loc, Map map, ThingDef casingMoteDef, float size = 1f)
        {
            if (!Controller.settings.ShowCasings || !loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(casingMoteDef, null);
            moteThrown.Scale = Rand.Range(0.5f, 0.3f) * size;
            moteThrown.exactRotation = Rand.Range(-3f, 4f);
            moteThrown.exactPosition = loc;
            moteThrown.airTimeLeft = 60;
            moteThrown.SetVelocity((float)Rand.Range(160, 200), Rand.Range(0.7f, 0.5f));
            //     moteThrown.SetVelocityAngleSpeed((float)Rand.Range(160, 200), Rand.Range(0.020f, 0.0115f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }
        #endregion

        #region Physics
        public const float gravityConst = 9.8f;
        
        /// <summary>
        /// Calculates the range reachable with a projectile of speed <i>velocity</i> fired at <i>angle</i> from height <i>shotHeight</i>. Does not take into account air resistance.
        /// </summary>
        /// <param name="velocity">Projectile velocity in cells per second.</param>
        /// <param name="angle">Shot angle in radians off the ground.</param>
        /// <param name="shotHeight">Height from which the projectile is fired in vertical cells.</param>
        /// <returns>Distance in cells that the projectile will fly at the given arc.</returns>
        public static float GetDistanceTraveled(float velocity, float angle, float shotHeight)
        {
        	if (shotHeight < 0.001f)
        	{
        		return (Mathf.Pow(velocity, 2f) / gravityConst) * Mathf.Sin(2f * angle);
        	}
        	return ((velocity * Mathf.Cos(angle)) / gravityConst) * (velocity * Mathf.Sin(angle) + Mathf.Sqrt(Mathf.Pow(velocity * Mathf.Sin(angle), 2f) + 2f * gravityConst * shotHeight));
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
        public static Vector2 GetDestination(Vector2 origin, float angle, float rotation, float velocity, float shotHeight)
        {
        	return origin + Vector2.up.RotatedBy(rotation) * GetDistanceTraveled(velocity, angle, shotHeight);
        }
        
        /// <summary>
        /// Calculates the time in seconds the arc characterized by <i>angle</i>, <i>shotHeight</i> takes to traverse at speed <i>velocity</i> - e.g until the height reaches zero. Does not take into account air resistance.
        /// </summary>
        /// <param name="velocity">Projectile velocity in cells per second.</param>
        /// <param name="angle">Shot angle in radians off the ground.</param>
        /// <param name="shotHeight">Height from which the projectile is fired in vertical cells.</param>
        /// <returns>Time in seconds that the projectile will take to traverse the given arc.</returns>
        public static float GetFlightTime(float velocity, float angle, float shotHeight)
        {
			//Calculates quadratic formula (g/2)t^2 + (-v_0y)t + (y-y0) for {g -> gravity, v_0y -> vSin, y -> 0, y0 -> shotHeight} to find t in fractional ticks where height equals zero.
			return (Mathf.Sin(angle) * velocity + Mathf.Sqrt(Mathf.Pow(Mathf.Sin(angle) * velocity, 2f) + 2f * gravityConst * shotHeight)) / gravityConst;
        }
        
        /// <summary>
        /// Calculates the shot angle necessary to reach <i>range</i> with a projectile of speed <i>velocity</i> at a height difference of <i>heightDifference</i>, returning either the upper or lower arc in radians. Does not take into account air resistance.
        /// </summary>
        /// <param name="velocity">Projectile velocity in cells per second.</param>
        /// <param name="range">Cells between shooter and target.</param>
        /// <param name="heightDifference">Difference between initial shot height and target height in vertical cells.</param>
        /// <param name="flyOverhead">Whether to take the lower (False) or upper (True) arc angle.</param>
        /// <returns>Arc angle in radians off the ground.</returns>
        public static float GetShotAngle(float velocity, float range, float heightDifference, bool flyOverhead)
        {
            return Mathf.Atan((Mathf.Pow(velocity, 2f) + (flyOverhead ? 1f : -1f) * Mathf.Sqrt(Mathf.Pow(velocity, 4f) - gravityConst * (gravityConst * Mathf.Pow(range, 2f) + 2f * heightDifference * Mathf.Pow(velocity, 2f)))) / (gravityConst * range));
        }

        public static Bounds GetBoundsFor(Thing thing)
        {
            if (thing == null)
            {
                return new Bounds();
            }
            var height = new CollisionVertical(thing);
            var width = GetCollisionWidth(thing) * 2;
            var thingPos = thing.DrawPos;
            thingPos.y = height.Max - height.HeightRange.Span / 2;
            Bounds bounds = new Bounds(thingPos, new Vector3(width, height.HeightRange.Span, width));
            return bounds;
        }

        /// <summary>
        /// Calculates the width of an object for purposes of bullet collision. Return value is distance from center of object to its edge in cells, so a wall filling out an entire cell has a width of 0.5.
        /// Also accounts for general body type, humanoids must be specified in the humanoidBodyList and will have reduced width relative to their overall body size.
        /// </summary>
        /// <param name="thing">The Thing to measure width of</param>
        /// <returns>Distance from center of Thing to its edge in cells</returns>
        public static float GetCollisionWidth(Thing thing)
        {
            Pawn pawn = thing as Pawn;
            if (pawn == null)
            {
                return 0.5f;    //Buildings, etc. fill out half a square to each side
            }
            return pawn.BodySize * GetCollisionBodyFactors(pawn).First;
        }

        /// <summary>
        /// Calculates body scale factors based on body type
        /// </summary>
        /// <param name="pawn">Which pawn to measure for</param>
        /// <returns>Width factor as First, height factor as second</returns>
        public static Pair<float, float> GetCollisionBodyFactors(Pawn pawn)
        {
            if (pawn == null)
            {
                Log.Error("CE calling GetCollisionBodyHeightFactor with nullPawn");
                return new Pair<float, float>(1, 1);
            }
            RacePropertiesExtensionCE props = pawn.def.GetModExtension<RacePropertiesExtensionCE>() ?? new RacePropertiesExtensionCE();
            BodyType type = props.bodyType;
#if DEBUG
            if (type == BodyType.Undefined) Log.ErrorOnce("CE returning BodyType Undefined for pawn " + pawn.ToString(),  35000198 + pawn.GetHashCode());
#else
            if (type == BodyType.Undefined) Log.Warning("CE returning BodyType Undefined for pawn " + pawn.ToString());
#endif
            switch (type)
            {
                case BodyType.Humanoid:
                    return new Pair<float, float>(0.25f, 1);
                case BodyType.Quadruped:
                    return new Pair<float, float>(0.5f, 0.5f);
                case BodyType.QuadrupedLow:
                    return new Pair<float, float>(0.5f, 0.25f);
                case BodyType.Serpentine:
                    return new Pair<float, float>(0.5f, 0.125f);
                case BodyType.Birdlike:
                    return new Pair<float, float>(0.5f, 0.75f);
                case BodyType.Monkeylike:
                    return new Pair<float, float>(0.25f, 0.5f);
                default:
                    return new Pair<float, float>(1, 1);
            }
        }

        /// <summary>
        /// Determines whether a pawn should be currently crouching down or not
        /// </summary>
        /// <returns>True for humanlike pawns currently doing a job during which they should be crouching down</returns>
        public static bool IsCrouching(this Pawn pawn)
        {
            return pawn.RaceProps.Humanlike && (pawn.CurJob?.def.GetModExtension<JobDefExtensionCE>()?.isCrouchJob ?? false);
        }

        public static bool IsTree(this Thing thing)
        {
            return thing.def.category == ThingCategory.Plant && thing.def.altitudeLayer == AltitudeLayer.Building;
        }

        #endregion Physics

        #region Inventory

        public static void TryUpdateInventory(Pawn pawn)
        {
            if (pawn != null)
            {
                CompInventory comp = pawn.TryGetComp<CompInventory>();
                if (comp != null)
                {
                    comp.UpdateInventory();
                }
            }
        }

        public static void TryUpdateInventory(ThingOwner owner)
        {
            Pawn pawn = owner?.Owner?.ParentHolder as Pawn;
            if (pawn != null)
            {
                TryUpdateInventory(pawn);
            }
        }

        #endregion
    }
}