using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class Verb_ShootCE_CIWS : Verb_ShootCE
    {
        protected int maximumPredectionTicks = 40;
        public override bool TryFindCEShootLineFromTo(IntVec3 root, LocalTargetInfo targ, out ShootLine resultingLine)
        {
            var targetComp = targ.Thing?.TryGetComp<CompCIWSTarget>();
            if (targetComp != null)
            {

            }
            if (targ.Thing is Skyfaller skyfaller)
            {

            }
            if (targ.Thing is ProjectileCE projectile)
            {
               
            }
            return base.TryFindCEShootLineFromTo(root, targ, out resultingLine);
        }
    }
}
