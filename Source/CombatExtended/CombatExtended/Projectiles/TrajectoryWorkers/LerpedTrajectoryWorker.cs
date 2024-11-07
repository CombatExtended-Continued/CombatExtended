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
            return Vector2.Lerp(origin, destination, ticks / startingTicksToImpact);
        }
    }
}
