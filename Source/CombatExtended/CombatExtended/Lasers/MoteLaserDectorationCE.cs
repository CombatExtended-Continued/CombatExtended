using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CombatExtended.Lasers
{
    public class MoteLaserDectorationCE : MoteThrown
    {
        public LaserBeamGraphicCE beam;
        public float baseSpeed;
        public float speedJitter;
        public float speedJitterOffset;

        public override float Alpha
        {
            get
            {
                Speed = (float) (baseSpeed + speedJitter * Math.Sin(Math.PI * (Find.TickManager.TicksGame*18f + speedJitterOffset) / 180.0));

                if (beam != null) return beam.Opacity;
                return base.Alpha;
            }
        }
    }
}
