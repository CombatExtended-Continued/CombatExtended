using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class SmartRocketTrajectoryWorker : BallisticsTrajectoryWorker
    {
        public override Vector3 MoveForward(LocalTargetInfo currentTarget, float shotRotation, float shotAngle, float gravityFactor, Vector2 origin, Vector3 exactPosition, ref Vector2 destination, float startingTicksToImpact, float shotHeight, ref bool kinit, ref Vector3 velocity, ref float shotSpeed, ref Vector3 curPosition, ref float mass, ref float ballisticCoefficient, ref float radius, ref float gravity, ref float initialSpeed, ref int flightTicks)
        {
            if (currentTarget.HasThing)
            {
                velocity = Vector3.RotateTowards(velocity, currentTarget.Thing.DrawPos, 360, 0); //Rotate rocket towards target. Not sure how it should work, haven't tested it yet
            }
            return base.MoveForward(currentTarget, shotRotation, shotAngle, gravityFactor, origin, exactPosition, ref destination, startingTicksToImpact, shotHeight, ref kinit, ref velocity, ref shotSpeed, ref curPosition, ref mass, ref ballisticCoefficient, ref radius, ref gravity, ref initialSpeed, ref flightTicks);
        }
    }
}
