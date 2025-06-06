using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public class JobDriver_Stabilize : JobDriver
    {
        private const float baseTendDuration = 60f;

        private Pawn Patient
        {
            get
            {
                return pawn.CurJob.targetA.Thing as Pawn;
            }
        }
        private Medicine Medicine
        {
            get
            {
                return pawn.CurJob.targetB.Thing as Medicine;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetA, job) && pawn.Reserve(TargetB, job);
        }

        public void MakeMedicineFilth(Medicine medicine)
        {
            var medExt = medicine.def.GetModExtension<MedicineFilthExtension>() ?? new MedicineFilthExtension();

            if (medExt.filthDefName != null && Rand.Chance(medExt.filthSpawnChance))
            {
                int filthQuantity = medExt.filthSpawnQuantity.RandomInRange;
                List<IntVec3> list = GenAdj.AdjacentCells8WayRandomized();
                for (int i = 0; i < filthQuantity; i++)
                {
                    IntVec3 cell = this.pawn.Position + list[i];
                    if (cell.InBounds(this.pawn.Map))
                    {
                        FilthMaker.TryMakeFilth(cell, this.pawn.Map, medExt.filthDefName);
                    }
                }
            }
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => Patient == null || Medicine == null);
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnDestroyedNullOrForbidden(TargetIndex.B);
            this.FailOnAggroMentalState(TargetIndex.A);
            this.AddEndCondition(delegate
            {
                if (Patient.health.hediffSet.GetHediffsTendable().Any(h => h.CanBeStabilized()))
                {
                    return JobCondition.Ongoing;
                }
                Medicine.Destroy();
                MakeMedicineFilth(Medicine);
                return JobCondition.Incompletable;
            });

            // Pick up medicine and haul to patient
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.A, null, false);
            // Stabilize patient
            int duration = (int)(1f / this.pawn.GetStatValue(StatDefOf.MedicalTendSpeed, true) * baseTendDuration);
            Toil waitToil = Toils_General.WaitWith(TargetIndex.A, duration, maintainPosture:true, maintainSleep: false).WithProgressBarToilDelay(TargetIndex.A).PlaySustainerOrSound(SoundDefOf.Interact_Tend);
            yield return waitToil;
            Toil stabilizeToil = new Toil();
            stabilizeToil.initAction = delegate
            {
                float xp = (!Patient.RaceProps.Animal) ? 125f : 50f * Medicine.def.MedicineTendXpGainFactor;
                pawn.skills.Learn(SkillDefOf.Medicine, xp);
                foreach (Hediff curInjury in from x in Patient.health.hediffSet.GetHediffsTendable() orderby x.BleedRate descending select x)
                {
                    if (curInjury.CanBeStabilized())
                    {
                        HediffComp_Stabilize comp = curInjury.TryGetComp<HediffComp_Stabilize>();
                        comp.Stabilize(pawn, Medicine);
                        break;
                    }
                }

            };
            stabilizeToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return stabilizeToil;
            yield return Toils_Jump.Jump(waitToil);
        }
    }
}
