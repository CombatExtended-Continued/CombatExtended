using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Verse;

namespace CombatExtended
{
    public class BallisticsTrajectoryWorker : BaseTrajectoryWorker
    {
        public override IEnumerable<Vector3> PredictPositions(ProjectileCE projectile, int tickCount)
        {
            if (tickCount > GenTicks.TicksPerRealSecond)
            {
                tickCount = GenTicks.TicksPerRealSecond;
            }
            var ep = projectile.ExactPosition;
            for (int i = 1; i < tickCount; i++)
            {
                yield return ep + projectile.velocity * i;
            }
        }

        public override Vector3 MoveForward(ProjectileCE projectile)
        {
            projectile.shotSpeed = GetSpeed(projectile.velocity);
            return BallisticMove(projectile);
        }
        protected virtual Vector3 BallisticMove(ProjectileCE projectile)
        {
            Accelerate(projectile);
            return projectile.ExactPosition + projectile.velocity;
        }
        protected virtual void Accelerate(ProjectileCE projectile)
        {
            AffectedByDrag(projectile);
            AffectedByGravity(projectile);
            ReactiveAcceleration(projectile);
        }

        protected virtual void ReactiveAcceleration(ProjectileCE projectile)
        {
            if (projectile.fuelTicks > 0)
            {
                projectile.fuelTicks--;
                projectile.velocity += projectile.velocity.normalized * projectile.Props.speedGain;
            }
        }

        protected void AffectedByGravity(ProjectileCE projectile)
        {
            projectile.velocity.y -= projectile.gravity / GenTicks.TicksPerRealSecond;
        }

        protected void AffectedByDrag(ProjectileCE projectile)
        {
            Vector3 velocity = projectile.velocity;
            float crossSectionalArea = projectile.radius;
            crossSectionalArea *= crossSectionalArea * Mathf.PI;
            // 2.5f is half the mass of 1m² x 1cell of air.
            var q = 2.5f * velocity.sqrMagnitude;
            var dragForce = q * crossSectionalArea / projectile.ballisticCoefficient;
            // F = mA
            // A = F / m
            var a = (float)-dragForce / projectile.mass;
            var normalized = velocity.normalized;
            if (a * a > velocity.sqrMagnitude)
            {
                a = -velocity.magnitude;
            }
            velocity.x += a * normalized.x;
            velocity.y += a * normalized.y;
            velocity.z += a * normalized.z;
        }
        public override Vector3 ExactPosToDrawPos(Vector3 exactPosition, int FlightTicks, int ticksToTruePosition, float altitude)
        {
            return exactPosition.WithY(altitude);
        }
    }
}
