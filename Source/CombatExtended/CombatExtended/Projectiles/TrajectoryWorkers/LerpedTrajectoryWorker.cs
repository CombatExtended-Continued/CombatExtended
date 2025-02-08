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
        public override IEnumerable<Vector3> NextPositions(ProjectileCE projectile)
        {
            var ticksToImpact = projectile.ticksToImpact;
            var origin = projectile.origin;
            var destination = projectile.Destination;
            var startingTicksToImpact = projectile.startingTicksToImpact;
            var shotHeight = projectile.shotHeight;
            var shotSpeed = projectile.shotSpeed;
            var shotAngle = projectile.shotAngle;
            var gravityFactor = projectile.GravityFactor;
            for (; ticksToImpact >= 0; ticksToImpact--)
            {
                var ticks = projectile.FlightTicks + (ticksToImpact - projectile.ticksToImpact);
                var v = Vec2Position(origin, destination, startingTicksToImpact, ticks);
                yield return new Vector3(v.x, GetHeightAtTicks(shotHeight, shotSpeed, shotAngle, ticks, gravityFactor), v.y);
            }
        }
        protected override void MoveForward(ProjectileCE projectile)
        {
            base.MoveForward(projectile);
            projectile.FlightTicks++;
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
