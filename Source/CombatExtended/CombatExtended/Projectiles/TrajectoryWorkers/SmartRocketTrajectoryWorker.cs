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
        protected override void ReactiveAcceleration(LocalTargetInfo currentTarget, float targetHeight, float speedGain, float maxSpeed, Vector3 exactPosition, ref Vector3 velocity, ref float shotSpeed)
        {
            if (currentTarget.ThingDestroyed)
            {
                base.ReactiveAcceleration(currentTarget, targetHeight, speedGain, maxSpeed, exactPosition, ref velocity, ref shotSpeed);
                return;
            }
            var targetPos = currentTarget.Thing?.DrawPos ?? currentTarget.Cell.ToVector3Shifted();
            targetPos = targetPos.WithY(targetHeight);
            var velocityChange = GetVelocity(speedGain, exactPosition, targetPos);
            shotSpeed = Mathf.Max(Mathf.Min(shotSpeed + speedGain, maxSpeed), 0f);
            velocity = GetVelocity(shotSpeed, Vector3.zero, velocity + velocityChange);
        }
        public override bool GuidedProjectile => true;
    }
}
