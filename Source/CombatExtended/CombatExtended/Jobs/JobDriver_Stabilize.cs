using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using UnityEngine;

namespace CombatExtended;
public class JobDriver_Stabilize : JobDriver
{
    private const float BaseTendDuration = 60f;
    private bool _didStabilize = false;
    private static List<Toil> tmpCollectToils = [];

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
        Scribe_Values.Look(ref _didStabilize, "didStabilize", defaultValue: false);
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
            if (_didStabilize)
            {

                MakeMedicineFilth(Medicine);
                var usedMedicine = pawn.CurJob.targetB.Thing;
                if (usedMedicine.stackCount > 1)
                {
                    usedMedicine.stackCount--;
                }
                else if (!usedMedicine.Destroyed)
                {
                    usedMedicine.Destroy();
                }
            }
        });
        Toil gotoToil = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
        List<Toil> list = CollectMedicineToils(pawn, Patient, job, gotoToil);
        foreach (Toil item in list)
        {
            yield return item;
        }
        yield return gotoToil;
        Toil carryMedicine = PickupMedicine(TargetIndex.B);
        yield return carryMedicine;
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
        // Stabilize patient
        int duration = (int)(1f / this.pawn.GetStatValue(StatDefOf.MedicalTendSpeed, true) * BaseTendDuration);
        Toil waitToil = Toils_General.WaitWith(TargetIndex.A, duration, true, true, false, TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A).PlaySustainerOrSound(SoundDefOf.Interact_Tend);
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
                        _didStabilize = true;
                        break;
                    }
                }

            },
            defaultCompleteMode = ToilCompleteMode.Instant
        };
        yield return stabilizeToil;
        yield return Toils_Jump.Jump(waitToil);
    }

    // Slightly modified version from JobDriver_Tend
    private static List<Toil> CollectMedicineToils(Pawn doctor, Pawn patient, Job job, Toil gotoToil)
    {
        tmpCollectToils.Clear();
        Thing medicineUsed = job.targetB.Thing;
        Pawn_InventoryTracker medicineHolderInventory = medicineUsed?.ParentHolder as Pawn_InventoryTracker;
        Pawn otherPawnMedicineHolder = job.targetA.Pawn;
        Toil reserveMedicine = Toils_Tend.ReserveMedicine(TargetIndex.B, patient).FailOnDespawnedNullOrForbidden(TargetIndex.B);
        // Skips all in List<Toil> if the doctor has the medicine
        tmpCollectToils.Add(Toils_Jump.JumpIf(gotoToil, () => medicineUsed != null && (doctor.inventory.Contains(medicineUsed)) || doctor.carryTracker.CarriedThing == medicineUsed));
        Toil toil = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOn(() => otherPawnMedicineHolder != medicineHolderInventory?.pawn || otherPawnMedicineHolder.IsForbidden(doctor));
        tmpCollectToils.Add(Toils_Haul.CheckItemCarriedByOtherPawn(medicineUsed, TargetIndex.A, toil));
        tmpCollectToils.Add(reserveMedicine);
        tmpCollectToils.Add(Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B));
        tmpCollectToils.Add(PickupMedicine(TargetIndex.B).FailOnDestroyedOrNull(TargetIndex.B));
        tmpCollectToils.Add(Toils_Haul.CheckForGetOpportunityDuplicate(reserveMedicine, TargetIndex.B, TargetIndex.None, takeFromValidStorage: true));
        tmpCollectToils.Add(Toils_Jump.Jump(gotoToil));
        tmpCollectToils.Add(toil);
        tmpCollectToils.Add(Toils_General.Wait(25).WithProgressBarToilDelay(TargetIndex.A));
        tmpCollectToils.Add(Toils_Haul.TakeFromOtherInventory(medicineUsed, doctor.inventory.innerContainer, medicineHolderInventory?.innerContainer, Medicine.GetMedicineCountToFullyHeal(patient), TargetIndex.B));
        return tmpCollectToils;
    }

    private static Toil PickupMedicine(TargetIndex medicine)
    {
        Toil toil = ToilMaker.MakeToil("PickupMedicine");
        toil.initAction = delegate
        {
            Pawn actor = toil.actor;
            Job curJob = actor.jobs.curJob;
            Thing thing = curJob.GetTarget(medicine).Thing;
            if (actor.carryTracker.CarriedThing != thing)
            {
                int toCarry = Mathf.Min(1, actor.Map.reservationManager.CanReserveStack(actor, thing, 1));
                if (toCarry > 0)
                {
                    actor.carryTracker.TryStartCarry(thing, toCarry);
                }
            }
            curJob.count = 0;
            if (thing.Spawned)
            {
                actor.Map.reservationManager.Release(thing, actor, curJob);
            }
            curJob.SetTarget(TargetIndex.B, actor.carryTracker.CarriedThing);
        };
        toil.defaultCompleteMode = ToilCompleteMode.Instant;
        return toil;
    }
}
