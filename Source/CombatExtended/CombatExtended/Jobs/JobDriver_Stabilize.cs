using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using UnityEngine;

namespace CombatExtended
{
    public class JobDriver_Stabilize : JobDriver
    {
        private const float BaseTendDuration = 60f;
        private Thing _usedMedicine;
        private bool didStabilize = false;

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

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref didStabilize, "didStabilize", defaultValue: false);
            Scribe_References.Look(ref _usedMedicine, "_usedMedicine");
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
                return JobCondition.Incompletable;
            });
            this.AddFinishAction(delegate
            {
                if (didStabilize)
                {
                    MakeMedicineFilth(Medicine);
                    if (_usedMedicine is { stackCount: > 0 })
                    {
                        _usedMedicine.SplitOff(1).Destroy();
                    }
                }
            });
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            // Stabilize patient
            int duration = (int)(1f / this.pawn.GetStatValue(StatDefOf.MedicalTendSpeed, true) * baseTendDuration);
            Toil carryMedicine = new Toil
            {
                initAction = delegate
                {
                    var curJob = pawn.jobs.curJob;
                    if (pawn.carryTracker.CarriedThing != Medicine)
                    {
                        int toCarry = Mathf.Min(1, pawn.Map.reservationManager.CanReserveStack(pawn, Medicine, 1));
                        if (toCarry > 0)
                        {
                            pawn.carryTracker.TryStartCarry(Medicine, toCarry);
                        }
                    }
                    curJob.count = 0;
                    if (Medicine.Spawned)
                    {
                        pawn.Map.reservationManager.Release(Medicine, pawn, job);
                    }
                    curJob.SetTarget(TargetIndex.B, pawn.carryTracker.CarriedThing);
                    _usedMedicine = pawn.carryTracker.CarriedThing;
                }
            };
            yield return carryMedicine;
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            if (pawn != Patient)
            {
                pawn.rotationTracker.FaceCell(Patient.Position);
            }
            // Stabilize patient
            int duration = (int)(1f / this.pawn.GetStatValue(StatDefOf.MedicalTendSpeed, true) * BaseTendDuration);
            Toil waitToil = Toils_General.WaitWith(TargetIndex.A, duration, maintainPosture: true, maintainSleep: false).WithProgressBarToilDelay(TargetIndex.A).PlaySustainerOrSound(SoundDefOf.Interact_Tend);
            yield return waitToil;
            Toil stabilizeToil = new Toil
            {
                initAction = delegate
                {
                    float xp = (!Patient.RaceProps.Animal) ? 125f : 50f * Medicine.def.MedicineTendXpGainFactor;
                    pawn.skills.Learn(SkillDefOf.Medicine, xp);
                    foreach (Hediff curInjury in from x in Patient.health.hediffSet.GetHediffsTendable() orderby x.BleedRate descending select x)
                    {
                        if (curInjury.CanBeStabilized())
                        {
                            HediffComp_Stabilize comp = curInjury.TryGetComp<HediffComp_Stabilize>();
                            comp.Stabilize(pawn, Medicine);
                            didStabilize = true;
                            break;
                        }
                    }

                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return stabilizeToil;
            yield return Toils_Jump.Jump(waitToil);
        }
    }
}
