using UnityEngine;
using Verse;

namespace CombatExtended;

/// <summary>
/// Trajectory worker for homing bullets (e.g. VWE2 VectorCompensator trait).
/// Uses angular steering to rotate the velocity toward the intended target each tick.
/// Steering ramps up gradually from zero over RampTicks to create a visible curve.
/// Unlike SmartRocketTrajectoryWorker, does not consume fuelTicks — guidance lasts the entire flight.
/// homingAcceleration is the max steering rate in radians per tick.
/// </summary>
public class HomingBulletTrajectoryWorker : BallisticsTrajectoryWorker
{
    public static readonly HomingBulletTrajectoryWorker Instance = new();

    /// <summary>
    /// Fixed number of ticks the bullet flies fully unguided.
    /// </summary>
    private const int ScatterTicks = 3;

    /// <summary>
    /// Number of ticks over which steering ramps from 0 to full strength after scatter phase.
    /// Creates a visible gradual curve instead of an instant snap to target.
    /// </summary>
    private const float RampTicks = 8f;

    protected override void ReactiveAcceleration(ProjectileCE projectile)
    {
        if (projectile.homingAcceleration <= 0f)
        {
            return;
        }

        // Scatter phase: fully unguided
        if (projectile.FlightTicks < ScatterTicks)
        {
            return;
        }

        LocalTargetInfo currentTarget = projectile.intendedTarget;
        if (currentTarget.ThingDestroyed)
        {
            return;
        }

        // Use map position for target, keeping projectile's current height (steer horizontally only;
        // the ballistic arc handles vertical). DrawPos.y is a render altitude layer, not physical height.
        var targetThing = currentTarget.Thing;
        var targetPos = targetThing != null
            ? targetThing.DrawPos
            : currentTarget.Cell.ToVector3Shifted();
        targetPos.y = projectile.ExactPosition.y;

        var delta = targetPos - projectile.ExactPosition;
        if (delta.sqrMagnitude < 0.01f)
        {
            return;
        }

        // Stop steering if bullet has passed the target (moving away)
        if (Vector3.Dot(projectile.velocity, delta) <= 0f)
        {
            return;
        }

        // Ramp steering from 0 to max over RampTicks for a gradual visible curve
        float t = Mathf.Clamp01((projectile.FlightTicks - ScatterTicks) / RampTicks);
        float currentSteering = projectile.homingAcceleration * t;

        // Angular steering: rotate velocity toward target by up to currentSteering radians per tick.
        var targetVelocity = delta.normalized * projectile.velocity.magnitude;
        projectile.velocity = Vector3.RotateTowards(projectile.velocity, targetVelocity, currentSteering, 0f);
    }

    public override bool GuidedProjectile => true;
}
