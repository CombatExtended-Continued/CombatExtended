using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class LerpedTrajectoryWorker_ExactPosDrawing : LerpedTrajectoryWorker
    {
        public override Vector3 ExactPosToDrawPos(Vector3 exactPosition, int FlightTicks, int ticksToTruePosition, float altitude)
        {
            return exactPosition.WithY(altitude);
        }
        public override Quaternion DrawRotation(ProjectileCE projectile)
        {
            return SpinIfNeeded(projectile, Quaternion.LookRotation((projectile.NextPositions.FirstOrDefault() - projectile.ExactPosition).Yto0()));
        }
        public override Quaternion ShadowRotation(ProjectileCE projectile)
        {
            return DrawRotation(projectile);
        }
    }
}
