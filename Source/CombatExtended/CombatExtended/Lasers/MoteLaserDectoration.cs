using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CombatExtended.Lasers
{
    class MoteLaserDectoration : MoteThrown
    {
        public LaserBeamGraphicCE beam = null;
        public float baseSpeed = 0;
        public float speedJitter = 0;
        public float speedJitterOffset = 0;

        public override float Alpha
        {
            get
            {
                Speed = (float)(baseSpeed + speedJitter * Math.Sin(Math.PI * (Find.TickManager.TicksGame * 18f + speedJitterOffset) / 180.0));

                if (beam != null) return beam.Opacity;
                return base.Alpha;
            }
        }
    }
}
