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
    public class LegacyTrajectoryWorker : BaseTrajectoryWorker
    {
        private ProjectileCE projectile;
        private bool exactPosition;
        private bool vec2Position;

        public LegacyTrajectoryWorker(ProjectileCE p, bool v2p, bool ep)
        {
            this.projectile = p;
            this.vec2Position = v2p;
            this.exactPosition = ep;
        }

        public override Vector3 MoveForward(ProjectileCE projectile)
        {
            if (exactPosition)
            {
                return this.projectile.ExactPosition;
            }

            var v2p = this.projectile.Vec2Position();
            return new Vector3(v2p.x, this.projectile.Height, v2p.y);
        }

        public override IEnumerable<Vector3> PredictPositions(ProjectileCE projectile, int tickCount, bool drawPos)
        {
            return new List<Vector3>();
        }

    }
}
