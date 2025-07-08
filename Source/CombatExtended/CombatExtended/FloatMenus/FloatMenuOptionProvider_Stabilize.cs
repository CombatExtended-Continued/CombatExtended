using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended;

public class FloatMenuOptionProvider_Stabilize : FloatMenuOptionProvider
{
    public override bool Drafted => true;
    public override bool Undrafted => true;
    public override bool Multiselect => false;
    public override bool MechanoidCanDo => false;
    public override bool RequiresManipulation => true;

    public override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
    {
        if (!IsValidTendTarget(context.FirstSelectedPawn, clickedPawn)) { return null; }
        if (!clickedPawn.health.hediffSet.GetHediffsTendable().Any(h => h.CanBeStabilized())) { return null; }
        if (context.FirstSelectedPawn.WorkTypeIsDisabled(WorkTypeDefOf.Doctor)) { return new FloatMenuOption("CE_CannotStabilize".Translate() + ": " + "IncapableOfCapacity".Translate(WorkTypeDefOf.Doctor.gerundLabel), null); }
        if (!context.FirstSelectedPawn.CanReach(clickedPawn, PathEndMode.ClosestTouch, Danger.Deadly)) { return new FloatMenuOption("CE_CannotStabilize".Translate() + ": " + "NoPath".Translate().CapitalizeFirst(), null); }
        return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("CE_Stabilize".Translate(clickedPawn.LabelCap), action: () => Stabilization_Utility.Stabilize(context.FirstSelectedPawn, clickedPawn)), context.FirstSelectedPawn, clickedPawn);

    }

    private bool IsValidTendTarget(Pawn doctor, Pawn patient) // Copy of vanilla DratedTend provider's
    {
        if (!doctor.Drafted && patient != doctor)
        {
            return false;
        }
        if (patient.Downed)
        {
            return true;
        }
        if (patient.HostileTo(doctor.Faction))
        {
            return false;
        }
        if (patient.IsColonist || patient.IsQuestLodger() || patient.IsPrisonerOfColony || patient.IsSlaveOfColony || (patient.Faction == Faction.OfPlayer && patient.IsAnimal))
        {
            return true;
        }
        if (patient.IsColonySubhuman && patient.mutant.Def.entitledToMedicalCare)
        {
            return true;
        }
        return false;
    }


}
