using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace CombatExtended.Compatibility.VGECompat;

public class DirectOverheadTrajectoryWorker : LerpedTrajectoryWorker
{
    public override float ShotAngle(ProjectilePropertiesCE projectilePropsCE, Vector3 source, Vector3 targetPos, float? speed = null)
    {
        if (projectilePropsCE.isInstant)
        {
            return base.ShotAngle(projectilePropsCE, source, targetPos, speed);
        }

        var targetHeight = targetPos.y;
        var shotHeight = source.y;
        var newTargetLoc = new Vector2(targetPos.x, targetPos.z);
        var sourceV2 = new Vector2(source.x, source.z);

        var _speed = speed ?? projectilePropsCE.speed;
        var gravityPerWidth = projectilePropsCE.GravityPerWidth;
        var heightDifference = targetHeight - shotHeight;
        var range = (newTargetLoc - sourceV2).magnitude;
        float squareRootCheck = Mathf.Sqrt(Mathf.Pow(_speed, 4f) - gravityPerWidth * (gravityPerWidth * Mathf.Pow(range, 2f) + 2f * heightDifference * Mathf.Pow(_speed, 2f)));
        if (float.IsNaN(squareRootCheck))
        {
            //Target is too far to hit with given velocity/range/gravity params
            //set firing angle for maximum distance
            Log.Warning("[CE] Tried to fire projectile to unreachable target cell, truncating to maximum distance.");
            return 45.0f * Mathf.Deg2Rad;
        }
        // Remove the projectilePropsCE.flyOverhead angle to get a direct shot
        return Mathf.Atan((Mathf.Pow(_speed, 2f) + -1f * squareRootCheck) / (gravityPerWidth * range));
    }
}
