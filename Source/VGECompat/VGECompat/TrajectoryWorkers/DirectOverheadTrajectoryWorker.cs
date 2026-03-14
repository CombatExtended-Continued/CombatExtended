using UnityEngine;

namespace CombatExtended.Compatibility.VGECompat;

public class DirectOverheadTrajectoryWorker : LerpedTrajectoryWorker
{
    public override float ShotAngle(ProjectilePropertiesCE projectilePropsCE, Vector3 source, Vector3 targetPos, float? speed = null)
    {
        if (projectilePropsCE.isInstant)
        {
            return base.ShotAngle(projectilePropsCE, source, targetPos, speed);
        }

        float? shotAngle = TryFindShotAngle(projectilePropsCE, source, targetPos, speed);
        if (shotAngle is float angle)
        {
            return angle;
        }

        return base.ShotAngle(projectilePropsCE, source, targetPos, speed);
    }

    private static float? TryFindShotAngle(ProjectilePropertiesCE projectilePropsCE, Vector3 source, Vector3 targetPos, float? speed)
    {
        float targetHeight = targetPos.y;
        float shotHeight = source.y;
        Vector2 newTargetLoc = new Vector2(targetPos.x, targetPos.z);
        Vector2 sourceV2 = new Vector2(source.x, source.z);

        float _speed = speed ?? projectilePropsCE.speed;
        float gravityPerWidth = projectilePropsCE.GravityPerWidth;
        float heightDifference = targetHeight - shotHeight;
        float range = (newTargetLoc - sourceV2).magnitude;
        float squareRootCheck = Mathf.Sqrt(Mathf.Pow(_speed, 4f) - gravityPerWidth * (gravityPerWidth * Mathf.Pow(range, 2f) + 2f * heightDifference * Mathf.Pow(_speed, 2f)));

        if (float.IsNaN(squareRootCheck))
        {
            return null;
        }

        // Remove the projectilePropsCE.flyOverhead angle to always get a direct shot
        return Mathf.Atan((Mathf.Pow(_speed, 2f) - squareRootCheck) / (gravityPerWidth * range));
    }
}
