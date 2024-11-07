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
                Log.Message($"{ticksToImpact}, {flightTicks}");
                yield return MoveForward(currentTarget, shotRotation, shotAngle, gravityFactor, origin, exactPosition, ref destination, ticksToImpact, startingTicksToImpact, shotHeight, ref kinit, ref velocity, ref shotSpeed, ref curPosition, ref mass, ref ballisticCoefficient, ref radius, ref gravity, ref initialSpeed, ref flightTicks);
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
    }
}
