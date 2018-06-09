using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public class JobGiver_ReactToSuppression : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            var comp = pawn.TryGetComp<CompSuppressable>();

            if (comp == null) return null;

            Job reactJob = SuppressionUtility.GetRunForCoverJob(pawn);

            // Hunker down
            if (reactJob == null && comp.IsHunkering)
            {
                LessonAutoActivator.TeachOpportunity(CE_ConceptDefOf.CE_Hunkering, pawn, OpportunityType.Critical);
                return new Job(CE_JobDefOf.HunkerDown, pawn) { checkOverrideOnExpire = true };
            }

            // Run for cover
            LessonAutoActivator.TeachOpportunity(CE_ConceptDefOf.CE_SuppressionReaction, pawn, OpportunityType.Critical);
            return reactJob;
        }
    }
}