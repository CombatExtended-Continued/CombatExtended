using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CombatExtended
{
    public class CompProperties_CIWS_Projectile : CompProperties_CIWS
    {
        public CompProperties_CIWS_Projectile()
        {
            compClass = typeof(CompCIWS_Projectile);
            this.label = "CE_CIWS_Projectile";
        }
        public bool interceptOnGroundProjectiles = false;
    }
}
