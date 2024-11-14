using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class LerpedTrajectoryWorker : BaseTrajectoryWorker
    {
        public override float DistanceTraveled(float shotHeight, float shotSpeed, float shotAngle, float GravityFactor)
        {
            return CE_Utility.MaxProjectileRange(shotHeight, shotSpeed, shotAngle, GravityFactor);
        }
        public override float GetFlightTime(float shotAngle, float shotSpeed, float GravityFactor, float shotHeight)
        {
            //Calculates quadratic formula (g/2)t^2 + (-v_0y)t + (y-y0) for {g -> gravity, v_0y -> vSin, y -> 0, y0 -> shotHeight} to find t in fractional ticks where height equals zero.
            return (Mathf.Sin(shotAngle) * shotSpeed + Mathf.Sqrt(Mathf.Pow(Mathf.Sin(shotAngle) * shotSpeed, 2f) + 2f * GravityFactor * shotHeight)) / GravityFactor;
        }
        public override Vector3 MoveForward(LocalTargetInfo currentTarget, float shotRotation, float shotAngle, float gravityFactor, Vector2 origin, Vector3 exactPosition, ref Vector2 destination, float tickToImpact, float startingTicksToImpact, float shotHeight, ref bool kinit, ref Vector3 velocity, ref float shotSpeed, ref Vector3 curPosition, ref float mass, ref float ballisticCoefficient, ref float radius, ref float gravity, ref float initialSpeed, ref int ticks)
        {
            ticks++;
            var v = Vec2Position(origin, destination, startingTicksToImpact, ticks);
            return new Vector3(v.x, GetHeightAtTicks(shotHeight, shotSpeed, shotAngle, ticks, gravityFactor), v.y);
        }
        protected float GetHeightAtTicks(float shotHeight, float shotSpeed, float shotAngle, int ticks, float gravityFactor)
        {
            var seconds = ((float)ticks) / GenTicks.TicksPerRealSecond;
            return (float)Math.Round(shotHeight + shotSpeed * Mathf.Sin(shotAngle) * seconds - (gravityFactor * seconds * seconds) / 2f, 3);
        }
        public Vector2 Vec2Position(Vector2 origin, Vector2 destination, float startingTicksToImpact, int ticks)
        {
            return Vector2.LerpUnclamped(origin, destination, ticks / startingTicksToImpact);
        }
    }
}
