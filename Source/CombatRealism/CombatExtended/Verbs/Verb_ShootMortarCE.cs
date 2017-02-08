using RimWorld;
using UnityEngine;
using Verse;
namespace CombatExtended
{
	public class Verb_ShootMortarCE : Verb_ShootCE
	{
        /*public override void WarmupComplete()
        {
            base.WarmupComplete();
            if (ShooterPawn != null && ShooterPawn.skills != null)
            {
                ShooterPawn.skills.Learn(SkillDefOf.Shooting, 100);
            }
        }*/
        public override ShiftVecReport ShiftVecReportFor(LocalTargetInfo target)
        {
            ShiftVecReport report = base.ShiftVecReportFor(target);
            report.circularMissRadius = this.GetMissRadiusForDist(report.shotDist);

            // Check for marker
            ArtilleryMarker marker = null;
            if (this.currentTarget.HasThing && this.currentTarget.Thing.HasAttachment(ThingDef.Named(ArtilleryMarker.MarkerDef)))
            {
                marker = (ArtilleryMarker)this.currentTarget.Thing.GetAttachment(ThingDef.Named(ArtilleryMarker.MarkerDef));
            }
            else
            {
                marker = (ArtilleryMarker)this.currentTarget.Cell.GetFirstThing(caster.Map, ThingDef.Named(ArtilleryMarker.MarkerDef));
            }
            if (marker != null)
            {
                report.aimingAccuracy = marker.aimingAccuracy;
                report.aimEfficiency = marker.aimEfficiency;
                report.weatherShift = marker.weatherShift;
                report.lightingShift = marker.lightingShift;

            }
            // If we don't have a marker check for indirect fire and apply penalty
            else if (report.shotDist > 107 || !GenSight.LineOfSight(this.caster.Position, report.target.Cell, caster.Map, true))
            {
                report.indirectFireShift = this.verbPropsCE.indirectFirePenalty * report.shotDist;
                report.weatherShift = 0f;
                report.lightingShift = 0f;
            }
            return report;
        }

        private float GetMissRadiusForDist(float targDist)
        {
            float maxRange = this.verbProps.range;
            if (this.compCharges != null)
            {
                Vector2 bracket;
                if (this.compCharges.GetChargeBracket(targDist, out bracket))
                {
                    maxRange = bracket.y;
                }
            }
            float rangePercent = targDist / maxRange;
            float missRadiusFactor = rangePercent <= 0.5f ? 1 - rangePercent : 0.5f + ((rangePercent - 0.5f) / 2);
            return this.verbProps.forcedMissRadius * missRadiusFactor;
        }
	}
}
