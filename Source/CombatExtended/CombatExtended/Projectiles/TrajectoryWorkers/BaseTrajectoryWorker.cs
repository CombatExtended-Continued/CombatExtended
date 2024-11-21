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
        public abstract Vector3 MoveForward(
            LocalTargetInfo currentTarget,
            float shotRotation,
            float shotAngle,
            float gravityFactor,
            Vector2 origin,
            Vector3 exactPosition,
            ref Vector2 destination,
            float tickToImpact,
            float startingTicksToImpact,
            float shotHeight,
            ref bool kinit,
            ref Vector3 velocity,
            ref float shotSpeed,
            ref Vector3 curPosition,
            ref float mass,
            ref float ballisticCoefficient,
            ref float radius,
            ref float gravity,
            ref float initialSpeed,
            ref int flightTicks
            );
        public virtual IEnumerable<Vector3> NextPositions(
            LocalTargetInfo currentTarget,
            float shotRotation,
            float shotAngle,
            float gravityFactor,
            Vector2 origin,
            Vector3 exactPosition,
            Vector2 destination,
            float ticksToImpact,
            float startingTicksToImpact,
            float shotHeight,
            bool kinit,
            Vector3 velocity,
            float shotSpeed,
            Vector3 curPosition,
            float mass,
            float ballisticCoefficient,
            float radius,
            float gravity,
            float initialSpeed,
            int flightTicks)
        {
            for (; ticksToImpact >= 0; ticksToImpact--)
            {
                yield return exactPosition = MoveForward(currentTarget, shotRotation, shotAngle, gravityFactor, origin, exactPosition, ref destination, ticksToImpact, startingTicksToImpact, shotHeight, ref kinit, ref velocity, ref shotSpeed, ref curPosition, ref mass, ref ballisticCoefficient, ref radius, ref gravity, ref initialSpeed, ref flightTicks);
            }
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
            return velocity.magnitude;
        }

        public virtual Vector3 GetVelocity(float shotSpeed, Vector3 origin, Vector3 destination)
        {
            return (destination - origin).normalized * shotSpeed / GenTicks.TicksPerRealSecond;
        }
        /// <summary>
        /// Get initial velocity
        /// </summary>
        /// <param name="shotSpeed">speed</param>
        /// <param name="rotation">rotation in degrees</param>
        /// <param name="angle">angle in radians</param>
        /// <returns></returns>
        public virtual Vector3 GetVelocity(float shotSpeed, float rotation, float angle)
        {
            angle = angle * Mathf.Rad2Deg; // transform to degrees
            return Vector2.up.RotatedBy(rotation).ToVector3().RotatedBy(angle) * shotSpeed / GenTicks.TicksPerRealSecond;
        }
    }
}
