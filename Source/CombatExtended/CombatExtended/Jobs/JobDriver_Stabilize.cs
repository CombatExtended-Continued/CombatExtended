using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace CombatExtended
{
    public class JobDriver_Stabilize : JobDriver
    {
        private const float baseTendDuration = 60f;

        private Pawn Patient { get { return pawn.CurJob.targetA.Thing as Pawn; } }
        private Medicine Medicine { get { return pawn.CurJob.targetB.Thing as Medicine; } }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetA, job) && pawn.Reserve(TargetB, job);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => Patient == null || Medicine == null);
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnDestroyedNullOrForbidden(TargetIndex.B);
            this.FailOnNotDowned(TargetIndex.A);
            this.AddEndCondition(delegate 
            {
                if (Patient.health.hediffSet.GetHediffsTendable().Any(h => h.CanBeStabilized())) return JobCondition.Ongoing;
                Medicine.Destroy();
                return JobCondition.Incompletable;
            });

            // Pick up medicine and haul to patient
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.A, null, false);

            // Stabilize patient
            int duration = (int)(1f / this.pawn.GetStatValue(StatDefOf.MedicalTendSpeed, true) * baseTendDuration);
            Toil waitToil = Toils_General.Wait(duration).WithProgressBarToilDelay(TargetIndex.A).PlaySustainerOrSound(SoundDefOf.Interact_Tend);
            yield return waitToil;
            Toil stabilizeToil = new Toil();
            stabilizeToil.initAction = delegate
            {
                float xp = (!Patient.RaceProps.Animal) ? 125f : 50f * Medicine.def.MedicineTendXpGainFactor;
                pawn.skills.Learn(SkillDefOf.Medicine, xp);
                foreach(Hediff curInjury in from x in Patient.health.hediffSet.GetHediffsTendable() orderby x.BleedRate descending select x)
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
