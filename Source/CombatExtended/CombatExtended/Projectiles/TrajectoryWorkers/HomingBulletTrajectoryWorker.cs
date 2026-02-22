using UnityEngine;
using Verse;

namespace CombatExtended;

/// <summary>
/// Trajectory worker for homing bullets (e.g. VWE2 VectorCompensator trait).
/// Steers the projectile toward its intended target using per-instance homingAcceleration.
/// Unlike SmartRocketTrajectoryWorker, does not consume fuelTicks and has an initial scatter phase.
/// </summary>
public class HomingBulletTrajectoryWorker : BallisticsTrajectoryWorker
{
    public static readonly HomingBulletTrajectoryWorker Instance = new();

    /// <summary>
    /// Fraction of total flight time during which the bullet flies unguided (natural scatter).
    /// </summary>
    private const float ScatterPhaseFraction = 0.3f;

    protected override void ReactiveAcceleration(ProjectileCE projectile)
    {
        if (projectile.homingAcceleration <= 0f)
        {
            return;
        }

        // Scatter phase: no guidance for the first 30% of flight
        if (projectile.startingTicksToImpact > 0f &&
            projectile.FlightTicks < projectile.startingTicksToImpact * ScatterPhaseFraction)
        {
            return;
        }

        LocalTargetInfo currentTarget = projectile.intendedTarget;
        if (currentTarget.ThingDestroyed)
        {
            return;
        }

        var targetPos = currentTarget.Thing?.DrawPos ?? currentTarget.Cell.ToVector3Shifted();
        var delta = targetPos - projectile.ExactPosition;
        projectile.velocity += delta.normalized * projectile.homingAcceleration / GenTicks.TicksPerRealSecond / GenTicks.TicksPerRealSecond;
    }

    public override bool GuidedProjectile => true;
}
