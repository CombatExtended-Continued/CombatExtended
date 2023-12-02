using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CombatExtended
{
    public class CompProperties_CIWS_Projectile : CompProperties
    {
        public CompProperties_CIWS_Projectile()
        {
            compClass = typeof(CompCIWS_Projectile);
        }
        public bool interceptOnGroundProjectiles = false;
        public bool radarRequired = false;
        public float hitChance = 0.33f;
    }
}
