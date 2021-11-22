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
        private int _lastConditionsCheckedAt = int.MinValue;
        private bool _conditionsMet = false;

        public override bool Available()
        {            
            return MarkingConditionsMet() && base.Available();
        }

        public override bool IsUsableOn(Thing target)
        {
            return MarkingConditionsMet() && base.IsUsableOn(target);
        }

        public void Dirty()
        {
            _lastConditionsCheckedAt = -1;
        }

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
            if (CasterIsPawn)
            {
                marker.caster = ShooterPawn;
            }
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

        public bool MarkingConditionsMet()
        {           
            if (_lastConditionsCheckedAt + GenTicks.TickRareInterval > GenTicks.TicksGame)
            {
                return _conditionsMet;
            }
            _lastConditionsCheckedAt = GenTicks.TicksGame;

            if (!CasterIsPawn)
            {                
                return _conditionsMet = false;
            }                                          
            if (ShooterPawn.apparel?.WornApparel.Any(a => a.def.IsRadioPack()) ?? false)
            {                
                return _conditionsMet = true;
            }
            TurretTracker tracker = caster.Map.GetComponent<TurretTracker>();

            if (tracker.Turrets.Any(t => t is Building_TurretGunCE turret && turret.IsMortar && turret.Faction == caster.Faction))
            {
                return _conditionsMet = true;
            }
            return _conditionsMet = false;
        }
    }
}
