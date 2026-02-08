using RimWorld;
using RimWorld.Planet;
using VanillaGravshipExpanded;
using Verse;

namespace CombatExtended.Compatibility.VGECompat;

public class Verb_ShootWithVGETargeting : Verb_ShootMortarCE
{
    public override bool TryCastShot()
    {
        if (caster is Building_GravshipTurretCE Turret)
        {
            // Reset targets if turret there is no manning pawn at terminal
            if (!Turret.Active || Turret.ManningPawn == null || Turret.linkedTerminal == null)
            {
                Turret.ResetForcedTarget();
                Turret.ResetCurrentTarget();
                return false;
            }
        } 
        return base.TryCastShot();
    }

    public override ShiftVecReport ShiftVecReportFor(GlobalTargetInfo target)
    {
        ShiftVecReport report = base.ShiftVecReportFor(target);
        Log.Message($"Original report: {report.aimingAccuracy} {report.sightsEfficiency}");

        report.circularMissRadius *= 0.5f;
        report.smokeDensity *= 0.5f;
        report.weatherShift *= 0.25f;
        report.lightingShift *= 0.25f;
        report.aimingAccuracy = 1f; // TODO: change
        report.sightsEfficiency = 2f; // TODO: Get user sights efficiency
        targetHasMarker = true; // add more precision for VGE's targeting system

        return report;
    }
}

