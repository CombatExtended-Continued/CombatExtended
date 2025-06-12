using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended;

public class Stabilization_Utility
{
    
    private readonly static List<Thing> AllMedicine = [];
    
    [global::CombatExtended.Compatibility.Multiplayer.SyncMethod]
    public static void Stabilize(Pawn pawn, Pawn patient)
    {
        bool pawnHasMedicine = pawn.inventory?.innerContainer?.Any(t => t.def.IsMedicine) ?? false;
        bool patientHasMedicine = patient.inventory?.innerContainer?.Any(t => t.def.IsMedicine) ?? false;
        bool pawnCarryingMedicine = pawn.carryTracker.CarriedThing?.def.IsMedicine ?? false;

        if (!pawnHasMedicine && !pawnCarryingMedicine && !patientHasMedicine)
        {
            if (TryFindNearbyMedicine(pawn, patient.Position, out Thing closestMedicine) || TryFindNearbyMedicine(pawn, pawn.Position, out closestMedicine))
            {
                AssignStabilizeJob(pawn, patient, closestMedicine);
            }
            else
            {
                Messages.Message("CE_CannotStabilize".Translate() + ": " + "CE_NoMedicine".Translate(pawn), patient, MessageTypeDefOf.RejectInput);
            }
            return;
        }

        Medicine bestMedicine = FindBestMedicine(pawn, patient, pawnHasMedicine, patientHasMedicine, pawnCarryingMedicine);
        if (bestMedicine != null)
        {
            AssignStabilizeJob(pawn, patient, bestMedicine);
        }
    }
    
    [global::CombatExtended.Compatibility.Multiplayer.SyncMethod]
    private static bool TryFindNearbyMedicine(Pawn pawn, IntVec3 position, out Thing closestMedicine)
    {
        closestMedicine = null;
        float closestDistSq = float.MaxValue;
        float maxDistance = Controller.settings.MedicineSearchRadiusSquared;
        AllMedicine.Clear();
        AllMedicine.AddRange(pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Medicine));
        for (int i = 0; i < AllMedicine.Count; i++)
        {
            Thing medicine = AllMedicine[i];
            if (!medicine.Spawned || medicine.IsForbidden(pawn) || !pawn.CanReserveAndReach(medicine, PathEndMode.ClosestTouch, Danger.Deadly))
            {
                continue;
            }
            var distance = medicine.Position.DistanceToSquared(position);
            if (distance <= maxDistance && distance < closestDistSq)
            {
                closestDistSq = distance;
                closestMedicine = medicine;
            }
        }
        return closestMedicine != null;
    }
    
    [global::CombatExtended.Compatibility.Multiplayer.SyncMethod]
    private static Medicine FindBestMedicine(Pawn pawn, Pawn patient, bool doctorHas, bool patientHas, bool doctorCarrying)
    {
        Medicine best = null;
        float potency = -1f;

        if (patientHas)
        {
            foreach (Thing t in patient.inventory.innerContainer)
            {
                TryUpdateBestMedicine(t, ref potency, ref best);
            }
        }
        if (doctorCarrying)
        {
            TryUpdateBestMedicine(pawn.carryTracker.CarriedThing, ref potency, ref best);
        }
        if (doctorHas)
        {
            foreach (Thing t in pawn.inventory.innerContainer)
            {
                TryUpdateBestMedicine(t, ref potency, ref best);
            }
        }
        return best;
    }
    
    [global::CombatExtended.Compatibility.Multiplayer.SyncMethod]
    private static void AssignStabilizeJob(Pawn pawn, Pawn patient, Thing medicine)
    {
        var job = JobMaker.MakeJob(CE_JobDefOf.Stabilize, patient, medicine);
        job.count = 1;
        pawn.jobs.TryTakeOrderedJob(job);
        PlayerKnowledgeDatabase.KnowledgeDemonstrated(CE_ConceptDefOf.CE_Stabilizing, KnowledgeAmount.Total);
    }
    
    [global::CombatExtended.Compatibility.Multiplayer.SyncMethod]
    private static void TryUpdateBestMedicine(Thing source, ref float bestPotency, ref Medicine bestMedicine)
    {
        if (source is Medicine medicine)
        {
            float potency = medicine.GetStatValue(StatDefOf.MedicalPotency);
            if (potency > bestPotency)
            {
                bestPotency = potency;
                bestMedicine = medicine;
            }
        }
    }
    
    [global::CombatExtended.Compatibility.Multiplayer.SyncMethod]
    private static void Pickup(Pawn pawn, Thing item)
    {
        item.SetForbidden(false, false);
        Job job = JobMaker.MakeJob(JobDefOf.TakeInventory, item);
        job.count = 1;
        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        pawn.Notify_HoldTrackerItem(item, 1);
        PlayerKnowledgeDatabase.KnowledgeDemonstrated(CE_ConceptDefOf.CE_InventoryWeightBulk, KnowledgeAmount.SpecificInteraction);
    }
    
    [global::CombatExtended.Compatibility.Multiplayer.SyncMethod]
    private static void PickupAll(Pawn pawn, Thing item)
    {
        item.SetForbidden(false, false);
        Job job = JobMaker.MakeJob(JobDefOf.TakeInventory, item);
        job.count = item.stackCount;
        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        pawn.Notify_HoldTrackerItem(item, item.stackCount);
        PlayerKnowledgeDatabase.KnowledgeDemonstrated(CE_ConceptDefOf.CE_InventoryWeightBulk, KnowledgeAmount.SpecificInteraction);
    }
    
    [global::CombatExtended.Compatibility.Multiplayer.SyncMethod]
    private static void PickupCount(Pawn pawn, Thing item, int selectCount)
    {
        item.SetForbidden(false, false);
        Job job = JobMaker.MakeJob(JobDefOf.TakeInventory, item);
        job.count = selectCount;
        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        pawn.Notify_HoldTrackerItem(item, selectCount);
    }
}
