using RimWorld;
using RimWorld.Planet;
using UnityEngine;
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

        if (caster is Building_GravshipTurretCE turret)
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
            float gravshipTurretTargetingFactor = turret.GravshipTargeting;

            // implementation of ShotReport_HitFactorFromShooter_Patch
            report.aimingAccuracy = turret.GetStatValue(StatDefOf.ShootingAccuracyTurret) * gravshipTurretTargetingFactor;
            // implementation of Verb_LaunchProjectile_ForcedMissRadius_Patch
            report.circularMissRadius = turret.GetLocalForcedMissRadius(report.circularMissRadius);
            // implementation of Verb_LaunchProjectile_GetForcedMissTarget_Patch
            report.circularMissRadius /= gravshipTurretTargetingFactor;

            // ------------------- //

        }
        else
        {
            // should never happen anyway (I think)
            Log.Warning("Only Building_GravshipTurretCE should use Verb_ShootWithVGETargeting");
        }
        return report;
    }
}

