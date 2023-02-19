using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using RimWorld;

namespace CombatExtended
{
    public class CompProperties_MechAmmo : CompProperties
    {
        [NoTranslate]
        public string gizmoIconSetMagCount;
        [NoTranslate]
        public string gizmoIconTakeAmmoNow;

        public CompProperties_MechAmmo()
        {
            this.compClass = typeof(CompMechAmmo);
        }
    }
}
