using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CombatExtended.Lasers
{
    public class LaserGunDef : ThingDef
    {
        public static LaserGunDef defaultObj = new LaserGunDef();

        public float barrelLength = 0.9f;
        public bool supportsColors = false;
    }
}
