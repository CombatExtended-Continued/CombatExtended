using RimWorld.Planet;
using Verse;

namespace CombatExtended.Compatibility.VGECompat;

public class Verb_ShootWithVGETargeting : Verb_ShootMortarCE
{
    // Give the ManningPawn of the VGE turret as Shooter, so its stats are used for the shot
    // Equivalent to VGE Verb_LaunchProjectile_TryCastShot_Patch
    public override Pawn CasterPawn => Caster is Building_GravshipTurretCE turret ? turret.ManningPawn : null;
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
        if (report == null)
        {
            return null;
        }

        if (caster is Building_GravshipTurretCE Turret)
        {
            if (!targetHasMarker)
            {
                // if there is no marker, ignore it and do like if there was one (VGE terminal should provide high tech precision after all...)
                targetHasMarker = true; // add more precision for VGE's targeting system
                report.circularMissRadius *= 0.5f;
                report.smokeDensity *= 0.5f;
                report.weatherShift *= 0.25f;
                report.lightingShift *= 0.25f;
            }

            // ------------------- //
            // I hope this will be balanced enough, else maybe we should take only 0.75 of it
            // To give an example : basic autonomous targeting system gives 0.9 aiming accuracy, and quest reward building gives 4.0
            report.aimingAccuracy = Turret.GravshipTargeting;
            // ------------------- //

            report.sightsEfficiency *= 2f; // Then I multiply by 2, because users use high tech terminal and it feels better
        }
        else
        {
            // should never happen anyway (I think)
            Log.Warning("Only Building_GravshipTurretCE should use Verb_ShootWithVGETargeting");
        }
        return report;
    }
}

