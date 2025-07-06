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
            ReactiveAcceleration(projectile);
            AffectedByDrag(projectile);
            AffectedByGravity(projectile);
        }

        protected virtual void ReactiveAcceleration(ProjectileCE projectile)
        {
            if (projectile.fuelTicks > 0)
            {
                // acceleration in cells / second / second to cells / tick / tick
                float acceleration = projectile.Props.speedGain / GenTicks.TicksPerRealSecond / GenTicks.TicksPerRealSecond;
                Vector3 v = projectile.velocity;
                projectile.fuelTicks--;
                v += v.normalized * acceleration;
                projectile.velocity = v;
            }
        }

        protected void AffectedByGravity(ProjectileCE projectile)
        {
            projectile.velocity.y -= projectile.GravityPerHeight / GenTicks.TicksPerRealSecond / GenTicks.TicksPerRealSecond;
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

        /// <summary>
        /// Shot angle in radians
        /// </summary>
        /// <param name="source">Source shot, including shot height</param>
        /// <param name="targetPos">Target position, including target height</param>
        /// <returns>angle in radians</returns>
        public override float ShotAngle(ProjectilePropertiesCE projectilePropsCE, Vector3 source, Vector3 targetPos, float? speed = null)
        {
            /* Distance in cells
             * Speed in cells / second
             * acceleration in cells / second / second
             * gravity in cells / second / second
             * fuelLimit in seconds
             */
            float D = (targetPos - source).magnitude;
            var initialSpeed = (speed ?? projectilePropsCE.speed);
            float acceleration = projectilePropsCE.speedGain;
            float fuelLimit = ((float)projectilePropsCE.fuelTicks) / GenTicks.TicksPerRealSecond;

            if (acceleration == 0 && fuelLimit == 0)
            {
                return base.ShotAngle(projectilePropsCE, source, targetPos, speed);
            }
            /* First calculate the distance covered while the thrust is still applied
             * Then find the average speed over the whole flight
             */
            var D_accel = initialSpeed * fuelLimit + 0.5f * acceleration * fuelLimit * fuelLimit;
            if (D_accel < D) // Fuel runs out before reaching the destination
            {
                float finalSpeed = initialSpeed + acceleration * fuelLimit;
                float D_remaining = D - D_accel;
                float time = fuelLimit + D_remaining / finalSpeed;
                float averageSpeed = D / time;
                return base.ShotAngle(projectilePropsCE, source, targetPos, averageSpeed);
            }
            else
            {
                var a = acceleration / 2;
                var b = initialSpeed;
                var c = -D;
                var discriminant = b * b - 4 * a * c;
                float time = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
                float averageSpeed = D / time;
                return base.ShotAngle(projectilePropsCE, source, targetPos, averageSpeed);
            }
        }
    }
}
