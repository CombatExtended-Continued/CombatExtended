using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended;

public class Pickup_Utility
{
    [global::CombatExtended.Compatibility.Multiplayer.SyncMethod]
    public static void Pickup(Pawn pawn, Thing item)
    {
        item.SetForbidden(false, false);
        Job job = JobMaker.MakeJob(JobDefOf.TakeInventory, item);
        job.count = 1;
        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        pawn.Notify_HoldTrackerItem(item, 1);
        PlayerKnowledgeDatabase.KnowledgeDemonstrated(CE_ConceptDefOf.CE_InventoryWeightBulk, KnowledgeAmount.SpecificInteraction);
    }

    [global::CombatExtended.Compatibility.Multiplayer.SyncMethod]
    public static void PickupAll(Pawn pawn, Thing item)
    {
        item.SetForbidden(false, false);
        Job job = JobMaker.MakeJob(JobDefOf.TakeInventory, item);
        job.count = item.stackCount;
        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        pawn.Notify_HoldTrackerItem(item, item.stackCount);
        PlayerKnowledgeDatabase.KnowledgeDemonstrated(CE_ConceptDefOf.CE_InventoryWeightBulk, KnowledgeAmount.SpecificInteraction);
    }

    [global::CombatExtended.Compatibility.Multiplayer.SyncMethod]
    public static void PickupCount(Pawn pawn, Thing item, int selectCount)
    {
        item.SetForbidden(false, false);
        Job job = JobMaker.MakeJob(JobDefOf.TakeInventory, item);
        job.count = selectCount;
        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        pawn.Notify_HoldTrackerItem(item, selectCount);
    }
}
