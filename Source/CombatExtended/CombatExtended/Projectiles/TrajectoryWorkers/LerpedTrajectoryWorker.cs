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
        public override IEnumerable<Vector3> PredictPositions(ProjectileCE projectile, int ticks)
        {
            if (projectile.cachedPredictedPositions != null && projectile.cachedPredictedPositions.Count >= ticks)
            {
                foreach (var p in projectile.cachedPredictedPositions)
                {
                    yield return p;
                }
            }
            else
            {
                var ticksToImpact = projectile.ticksToImpact;
                var startingTicksToImpact = projectile.startingTicksToImpact;
                var end = (projectile.FlightTicks - startingTicksToImpact < ticks) ? projectile.FlightTicks - startingTicksToImpact : ticks;

                for (int ticksOffset = 1; ticksOffset <= end; ticksOffset++)
                {
                    var tick = projectile.FlightTicks + ticksOffset;
                    yield return GetPositionAtTick(projectile, tick);
                }
            }
        }
        public override Vector3 MoveForward(ProjectileCE projectile)
        {
            projectile.FlightTicks++;
            return GetPositionAtTick(projectile, projectile.FlightTicks);
        }
        protected Vector3 GetPositionAtTick(ProjectileCE projectile, int tick)
        {
            var v = Vec2Position(projectile.origin, projectile.Destination, projectile.startingTicksToImpact, tick);
            return new Vector3(v.x, GetHeightAtTicks(projectile.shotHeight, projectile.shotSpeed, projectile.shotAngle, tick, projectile.GravityFactor), v.y);
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
