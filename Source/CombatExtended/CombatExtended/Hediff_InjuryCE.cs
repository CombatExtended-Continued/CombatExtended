using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class Hediff_InjuryCE : Hediff_Injury
    {
        public override float BleedRate
        {
            get
            {
                float bleedRate = base.BleedRate;
                if (bleedRate > 0)
                {
                    // Check for stabilized comp
                    HediffComp_Stabilize comp = this.TryGetComp<HediffComp_Stabilize>();
                    if (comp != null)
                    {
                        return bleedRate * comp.BleedModifier;
                    }
                }
                return bleedRate;
            }
        }

        public bool CanBeStabilized()
        {
            if (this.IsTended() || this.IsOld()) return false;
            HediffComp_Stabilize comp = this.TryGetComp<HediffComp_Stabilize>();
            return comp != null && !comp.Stabilized;
        }
    }
}
