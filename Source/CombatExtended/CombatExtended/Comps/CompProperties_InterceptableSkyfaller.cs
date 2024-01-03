using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CombatExtended
{
    public class CompProperties_InterceptableSkyfaller : CompProperties
    {
        public CompProperties_InterceptableSkyfaller()
        {
            compClass = typeof(Comp_InterceptableSkyfaller);
        }
        public float HP = 100f;
        public float sharpArmor = 10f;
        public float instantDestroyChance = 0.00f;
        public float chanceToHitContainingThings = 0.7f;
    }
}
