using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace CombatExtended;
public class SmartRocketTrajectoryWorker : BallisticsTrajectoryWorker
{
    protected override void ReactiveAcceleration(ProjectileCE projectile)
    {
        LocalTargetInfo currentTarget = projectile.intendedTarget;
        if (currentTarget.ThingDestroyed)
        {
            base.ReactiveAcceleration(projectile);
            return;
        }
        if (projectile.fuelTicks < 1)
        {
            return;
        }
        projectile.fuelTicks--;
        var targetPos = currentTarget.Thing?.DrawPos ?? currentTarget.Cell.ToVector3Shifted();
        var delta = targetPos - projectile.ExactPosition;
        projectile.velocity += delta.normalized * projectile.Props.speedGain / GenTicks.TicksPerRealSecond / GenTicks.TicksPerRealSecond;
    }
    public override bool GuidedProjectile => true;
}
