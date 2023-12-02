using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CombatExtended
{
    public class CompProperties_CIWS_Skyfaller : CompProperties
    {
        public CompProperties_CIWS_Skyfaller()
        {
            compClass = typeof(CompCIWS_Skyfaller);
        }
        public float hitChance = 0.2f;
    }
}
