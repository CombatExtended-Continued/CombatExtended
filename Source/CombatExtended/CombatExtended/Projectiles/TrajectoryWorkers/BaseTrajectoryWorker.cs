using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public abstract class BaseTrajectoryWorker
    {
        public abstract Vector3 MoveForward(ProjectileCE projectile);

        public abstract IEnumerable<Vector3> PredictPositions(ProjectileCE projectile, int tickCount, bool drawPos);

        public virtual void NotifyTicked(ProjectileCE projectile)
        {
            projectile.cachedPredictedPositions = null;
        }

        public virtual Vector3 ExactPosToDrawPos(Vector3 exactPosition, int FlightTicks, int ticksToTruePosition, float altitude)
        {
            var sh = Mathf.Max(0f, (exactPosition.y) * 0.84f);
            if (FlightTicks < ticksToTruePosition)
            {
                sh *= (float)FlightTicks / ticksToTruePosition;
            }
            return new Vector3(exactPosition.x, altitude, exactPosition.z + sh);
        }
        public virtual Vector2 Destination(Vector2 origin, float shotRotation, float shotHeight, float shotSpeed, float shotAngle, float GravityFactor) => origin + Vector2.up.RotatedBy(shotRotation) * DistanceTraveled(shotHeight, shotSpeed, shotAngle, GravityFactor);
        public virtual float DistanceTraveled(float shotHeight, float shotSpeed, float shotAngle, float GravityFactor)
        {
            return CE_Utility.MaxProjectileRange(shotHeight, shotSpeed, shotAngle, GravityFactor);
        }
        public virtual float GetFlightTime(float shotAngle, float shotSpeed, float GravityFactor, float shotHeight)
        {
            //Calculates quadratic formula (g/2)t^2 + (-v_0y)t + (y-y0) for {g -> gravity, v_0y -> vSin, y -> 0, y0 -> shotHeight} to find t in fractional ticks where height equals zero.
            return (Mathf.Sin(shotAngle) * shotSpeed + Mathf.Sqrt(Mathf.Pow(Mathf.Sin(shotAngle) * shotSpeed, 2f) + 2f * GravityFactor * shotHeight)) / GravityFactor;
        }

        public virtual float GetSpeed(Vector3 velocity)
        {
            return velocity.magnitude * GenTicks.TicksPerRealSecond;
        }

        public virtual Vector3 GetVelocity(float shotSpeed, Vector3 origin, Vector3 destination)
        {
            return (destination - origin).normalized * shotSpeed / GenTicks.TicksPerRealSecond;
        }
        /// <summary>
        /// Get initial velocity
        /// </summary>
        /// <param name="shotSpeed">speed (meters / second)</param>
        /// <param name="rotation">rotation in degrees</param>
        /// <param name="angle">angle in radians</param>
        /// <returns></returns>
        public virtual Vector3 GetInitialVelocity(float shotSpeed, float rotation, float angle)
        {
            rotation = (rotation + 90) * Mathf.Deg2Rad;
            var ss = (shotSpeed / GenTicks.TicksPerRealSecond); // Speed in cells / tick
            return new Vector3(Mathf.Cos(rotation) * Mathf.Cos(angle) * ss,
                               Mathf.Sin(angle) * ss,
                               Mathf.Sin(rotation) * Mathf.Cos(angle) * ss);
        }

        /// <summary>
        /// Shot angle in radians
        /// </summary>
        /// <param name="source">Source shot, including shot height</param>
        /// <param name="targetPos">Target position, including target height</param>
        /// <param name="speed">speed (cells / second)</param>
        /// <returns>angle in radians</returns>
        public virtual float ShotAngle(ProjectilePropertiesCE projectilePropsCE, Vector3 source, Vector3 targetPos, float? speed = null)
        {
            var targetHeight = targetPos.y;
            var shotHeight = source.y;
            var newTargetLoc = new Vector2(targetPos.x, targetPos.z);
            var sourceV2 = new Vector2(source.x, source.z);
            if (projectilePropsCE.isInstant)
            {
                return Mathf.Atan2(targetHeight - shotHeight, (newTargetLoc - sourceV2).magnitude);
            }
            else
            {
                var _speed = speed ?? projectilePropsCE.speed;
                var gravity = projectilePropsCE.Gravity;
                var heightDifference = targetHeight - shotHeight;
                var range = (newTargetLoc - sourceV2).magnitude;
                float squareRootCheck = Mathf.Sqrt(Mathf.Pow(_speed, 4f) - gravity * (gravity * Mathf.Pow(range, 2f) + 2f * heightDifference * Mathf.Pow(_speed, 2f)));
                if (float.IsNaN(squareRootCheck))
                {
                    //Target is too far to hit with given velocity/range/gravity params
                    //set firing angle for maximum distance
                    Log.Warning("[CE] Tried to fire projectile to unreachable target cell, truncating to maximum distance.");
                    return 45.0f * Mathf.Deg2Rad;
                }
                return Mathf.Atan((Mathf.Pow(_speed, 2f) + (projectilePropsCE.flyOverhead ? 1f : -1f) * squareRootCheck) / (gravity * range));
            }
        }
        public virtual float ShotRotation(ProjectilePropertiesCE projectilePropertiesCE, Vector3 source, Vector3 targetPos)
        {
            var w = targetPos - source;
            return (-90 + Mathf.Rad2Deg * Mathf.Atan2(w.z, w.x)) % 360;
        }
        public virtual bool GuidedProjectile => false;
    }
}
