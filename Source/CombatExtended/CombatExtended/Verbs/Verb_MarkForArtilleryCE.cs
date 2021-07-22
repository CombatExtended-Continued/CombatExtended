using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class Verb_MarkForArtillery : Verb_LaunchProjectileCE
    {
        public override void WarmupComplete()
        {
            base.WarmupComplete();
            if (ShooterPawn != null && ShooterPawn.skills != null)
            {
                ShooterPawn.skills.Learn(SkillDefOf.Shooting, 200);
            }
        }

        public override bool TryCastShot()
        {
            ArtilleryMarker marker = ThingMaker.MakeThing(ThingDef.Named(ArtilleryMarker.MarkerDef)) as ArtilleryMarker;
            ShiftVecReport report = ShiftVecReportFor(currentTarget);
            marker.sightsEfficiency = report.sightsEfficiency;
            marker.aimingAccuracy = report.aimingAccuracy;
            marker.lightingShift = report.lightingShift;
            marker.weatherShift = report.weatherShift;

            GenSpawn.Spawn(marker, this.currentTarget.Cell, caster.Map);

            // Check for something to attach marker to
            if (this.currentTarget.HasThing)
            {
                CompAttachBase comp = this.currentTarget.Thing.TryGetComp<CompAttachBase>();
                if (comp != null)
                {
                    marker.AttachTo(this.currentTarget.Thing);
                }
            }
            // Show we learned something
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(CE_ConceptDefOf.CE_Spotting, KnowledgeAmount.SmallInteraction);

            return true;
        }

        public override void VerbTickCE()
        {
            if (CasterPawn != null && CasterPawn.IsColonistPlayerControlled)
            {
                LessonAutoActivator.TeachOpportunity(CE_ConceptDefOf.CE_Spotting, OpportunityType.GoodToKnow);
            }
        }
    }
}
