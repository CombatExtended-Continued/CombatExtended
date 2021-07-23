using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class Verb_ShootFlareCE : Verb_ShootMortarCE
    {
        private const float MISSRADIUS_FACTOR = 0.4f;
        private const float DISTFACTOR_FACTOR = 0.9f;

        public override ShiftVecReport ShiftVecReportFor(LocalTargetInfo target)
        {
            ShiftVecReport report = base.ShiftVecReportFor(target);
            report.shotDist = Vector3.Distance(target.CenterVector3, caster.TrueCenter()) * DISTFACTOR_FACTOR;
            return report;
        }

        protected override float GetMissRadiusForDist(float targDist)
        {
            return base.GetMissRadiusForDist(targDist) * MISSRADIUS_FACTOR;
        }
    }
}
