using RimWorld;
using UnityEngine;
using Verse;
namespace CombatExtended
{
    public class Verb_ShootMortarCE : Verb_ShootCE
    {
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
            else if (currentTarget.Cell.InBounds(caster.Map))
            {
                marker = (ArtilleryMarker)this.currentTarget.Cell.GetFirstThing(caster.Map, ThingDef.Named(ArtilleryMarker.MarkerDef));
            }
            if (marker != null)
            {
                report.aimingAccuracy = marker.aimingAccuracy;
                report.sightsEfficiency = marker.sightsEfficiency;
                report.weatherShift = marker.weatherShift;
                report.lightingShift = marker.lightingShift;
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(CE_ConceptDefOf.CE_Spotting, KnowledgeAmount.SpecificInteraction);
            }
            // If we don't have a marker check for indirect fire and apply penalty
            else if (report.shotDist > 75 || !GenSight.LineOfSight(this.caster.Position, report.target.Cell, caster.Map, true))
            {
                report.indirectFireShift = this.VerbPropsCE.indirectFirePenalty * report.shotDist;
                report.weatherShift = 0f;
                report.lightingShift = 0f;
            }
            return report;
        }

        private float GetMissRadiusForDist(float targDist)
        {
            float maxRange = this.verbProps.range;
            if (this.CompCharges != null)
            {
                Vector2 bracket;
                if (this.CompCharges.GetChargeBracket(targDist, ShotHeight, projectilePropsCE.Gravity, out bracket))
                {
                    maxRange = bracket.y;
                }
            }
            float rangePercent = targDist / maxRange;
            float missRadiusFactor = rangePercent <= 0.5f ? 1 - rangePercent : 0.5f + ((rangePercent - 0.5f) / 2);
            return VerbPropsCE.circularError * missRadiusFactor;
        }
    }
}
