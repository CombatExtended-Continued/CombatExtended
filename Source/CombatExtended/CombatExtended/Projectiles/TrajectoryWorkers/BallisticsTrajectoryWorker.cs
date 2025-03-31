﻿using System;
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
        public override IEnumerable<Vector3> NextPositions(ProjectileCE projectile)
        {
            var ticksToImpact = projectile.ticksToImpact;
            var exactPosition = projectile.ExactPosition;
            LocalTargetInfo currentTarget = projectile.intendedTarget;
            var props = projectile.Props;
            float speedGain = props.speedGain;
            float maxSpeed = props.speed;
            Vector3 velocity = projectile.velocity;
            float shotSpeed = props.speed;
            float mass = projectile.mass;
            float ballisticCoefficient = projectile.ballisticCoefficient;
            float radius = projectile.radius;
            float gravity = projectile.gravity;
            for (int ticksOffset = 1; ticksOffset <= ticksToImpact; ticksOffset++)
            {
                yield return exactPosition = BallisticMove(currentTarget, exactPosition, speedGain, maxSpeed, ref velocity, ref shotSpeed, ref mass, ref ballisticCoefficient, ref radius, ref gravity);
            }
        }
        protected override void MoveForward(ProjectileCE projectile)
        {
            projectile.ExactPosition = BallisticMove(projectile.intendedTarget, projectile.ExactPosition, projectile.Props.speedGain, projectile.Props.speed, ref projectile.velocity, ref projectile.shotSpeed, ref projectile.mass, ref projectile.ballisticCoefficient, ref projectile.radius, ref projectile.gravity);
            projectile.FlightTicks++;
        }
        protected virtual Vector3 BallisticMove(LocalTargetInfo currentTarget, Vector3 exactPosition, float speedGain, float maxSpeed, ref Vector3 velocity, ref float shotSpeed, ref float mass, ref float ballisticCoefficient, ref float radius, ref float gravity)
        {
            Accelerate(currentTarget, radius, ballisticCoefficient, mass, gravity, speedGain, maxSpeed, exactPosition, ref velocity, ref shotSpeed);
            shotSpeed = GetSpeed(velocity);
            return exactPosition + velocity;
        }
        protected virtual void Accelerate(LocalTargetInfo currentTarget, float radius, float ballisticCoefficient, float mass, float gravity, float speedGain, float maxSpeed, Vector3 exactPosition, ref Vector3 velocity, ref float shotSpeed)
        {
            AffectedByDrag(radius, shotSpeed, ballisticCoefficient, mass, ref velocity);
            AffectedByGravity(gravity, ref velocity);
            ReactiveAcceleration(currentTarget, speedGain, maxSpeed, exactPosition, ref velocity, ref shotSpeed);
        }

        protected virtual void ReactiveAcceleration(LocalTargetInfo currentTarget, float speedGain, float maxSpeed, Vector3 exactPosition, ref Vector3 velocity, ref float shotSpeed)
        {
            var speedChange = Mathf.Max(Mathf.Min(maxSpeed - shotSpeed, speedGain), 0f);
            if (speedChange > 0.001f)
            {
                velocity = velocity + GetVelocity(speedChange, Vector3.zero, velocity);
            }
        }

        protected void AffectedByGravity(float gravity, ref Vector3 velocity)
        {
            var original = velocity;
            velocity.y -= gravity / GenTicks.TicksPerRealSecond;
        }

        protected void AffectedByDrag(float radius, float shotSpeed, float ballisticCoefficient, float mass, ref Vector3 velocity)
        {
            float crossSectionalArea = radius;
            crossSectionalArea *= crossSectionalArea * 3.14159f;
            // 2.5f is half the mass of 1m² x 1cell of air.
            var q = 2.5f * shotSpeed * shotSpeed;
            var dragForce = q * crossSectionalArea / ballisticCoefficient;
            // F = mA
            // A = F / m
            var a = (float)-dragForce / mass;
            var normalized = velocity.normalized;
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
