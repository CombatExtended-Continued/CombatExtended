using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CombatExtended
{
    public class RecoilAdjustExtension : DefModExtension
    {
        public float recoilModifier = 1;
        public float recoilScale = -1;
        public int recoilTick = -1;
    }
}
