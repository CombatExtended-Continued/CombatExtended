using System;
using Verse;

namespace CombatExtended
{
    public class Verb_ShootFlareCE : Verb_ShootMortarCE
    {
        public override ShiftVecReport ShiftVecReportFor(LocalTargetInfo target)
        {
            ShiftVecReport report = base.ShiftVecReportFor(target);
            report.shotDist *= 2;
            return report;
        }
    }
}
