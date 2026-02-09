using RimWorld.Planet;
using System;
using System.Data.SqlTypes;
using Verse;
using static UnityEngine.GraphicsBuffer;

namespace CombatExtended.Compatibility.VGECompat;

public class Verb_ShootWithVGETargeting : Verb_ShootMortarCE
{
    public override bool TryCastShot()
    {
        if (caster is Building_GravshipTurretCE turret)
        {
            if (!turret.Active || !turret.CanFire)
            {
                turret.ResetForcedTarget();
                turret.ResetCurrentTarget();
                return false;
            }

            return base.TryCastShot();
        }

        Log.Error("Only Building_GravshipTurretCE should use Verb_ShootWithVGETargeting");
        return false;
    }

    public override ShiftVecReport ShiftVecReportFor(GlobalTargetInfo target)
    {
        ShiftVecReport report = base.ShiftVecReportFor(target);
        if (caster is Building_GravshipTurretCE Turret)
        {
            if (!targetHasMarker)
            {
                // if there is no marker, ignore it and do like if there was.
                targetHasMarker = true; // add more precision for VGE's targeting system
                report.circularMissRadius *= 0.5f;
                report.smokeDensity *= 0.5f;
                report.weatherShift *= 0.25f;
                report.lightingShift *= 0.25f;
            }

            // ------------------- //
            // Hope this will be balanced enough, else maybe take only 0.75 of it
            // To give example : basic autonomous targeting system gives 0.9 aiming accuracy, and quest reward building gives 4.0
            report.aimingAccuracy = Turret.GravshipTargeting;
            // ------------------- //

            report.sightsEfficiency *= 2f; // I multiply by 2, because users use high tech terminal
        }
        else
        {
            // should not happen anyway (I guess)
            Log.Warning("Only Building_GravshipTurretCE should use Verb_ShootWithVGETargeting");
        }
        return report;
    }
}

