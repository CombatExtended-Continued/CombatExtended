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
            List<Vector3> cachedPredictedPositions = projectile.cachedPredictedPositions ?? new List<Vector3>();
            var ticksToImpact = projectile.ticksToImpact;
            var end = (ticksToImpact < ticks) ? ticksToImpact : ticks;
            if (cachedPredictedPositions.Count < end)
            {
                for (int ticksOffset = cachedPredictedPositions.Count; ticksOffset < end; ticksOffset++)
                {
                    var tick = projectile.FlightTicks + ticksOffset;
                    cachedPredictedPositions.Add(GetPositionAtTick(projectile, tick));
                }
            }
            return cachedPredictedPositions;
        }
        public override void NotifyTicked(ProjectileCE projectile)
        {
            // Don't invalidate the cache, as our MoveForward handles dequeing it.
        }

        public override Vector3 MoveForward(ProjectileCE projectile)
        {
            // Someone has asked us to predict our positions before. It's probably faster to just recalculate, but this handles shifting the cached values out without rebuilding the whole cache.
            if (projectile.cachedPredictedPositions != null && projectile.cachedPredictedPositions.Count > 0)
            {
                Vector3 np = projectile.cachedPredictedPositions[0];
                projectile.cachedPredictedPositions.RemoveAt(0);
                return np;
            }
            return GetPositionAtTick(projectile, projectile.FlightTicks);
        }
        protected Vector3 GetPositionAtTick(ProjectileCE projectile, int tick)
        {
            var v = Vec2Position(projectile.origin, projectile.Destination, projectile.startingTicksToImpact, tick);
            return new Vector3(v.x, GetHeightAtTicks(projectile.shotHeight, projectile.shotSpeed, projectile.shotAngle, tick, projectile.GravityPerWidth), v.y);
        }
        protected float GetHeightAtTicks(float shotHeight, float shotSpeed, float shotAngle, int ticks, float gravityPerWidth)
        {
            var seconds = ((float)ticks) / GenTicks.TicksPerRealSecond;
            return (float)Math.Round(shotHeight + shotSpeed * Mathf.Sin(shotAngle) * seconds - (gravityPerWidth * seconds * seconds) / 2f, 3);
        }
        public Vector2 Vec2Position(Vector2 origin, Vector2 destination, float startingTicksToImpact, int ticks)
        {
            return Vector2.LerpUnclamped(origin, destination, ticks / startingTicksToImpact);
        }
    }
}
