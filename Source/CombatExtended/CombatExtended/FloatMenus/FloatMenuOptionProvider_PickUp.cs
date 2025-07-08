using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended;

public class FloatMenuOptionProvider_PickUp : FloatMenuOptionProvider
{
    public override bool Drafted => true;
    public override bool Undrafted => true;
    public override bool Multiselect => false;
    public override bool MechanoidCanDo => true;

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        CompInventory inventory = context.FirstSelectedPawn.TryGetComp<CompInventory>();
        if (inventory == null) { yield break; }
        if (!clickedThing.def.alwaysHaulable || clickedThing is Corpse) { yield break; }

        int count = 0;
        if (!context.FirstSelectedPawn.CanReach(clickedThing, PathEndMode.Touch, Danger.Deadly))
        {
            yield return new FloatMenuOption("CannotPickUp".Translate(clickedThing.LabelShort, clickedThing) + " (" + "NoPath".Translate() + ")", null);
        }
        else if (!inventory.CanFitInInventory(clickedThing, out count))
        {
            yield return new FloatMenuOption("CannotPickUp".Translate(clickedThing.LabelShort, clickedThing) + " (" + "CE_InventoryFull".Translate() + ")", null);
        }
        else if (count == 1)
        {
            yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("PickUpOne".Translate(clickedThing.Label, clickedThing), () => Pickup_Utility.Pickup(context.FirstSelectedPawn, clickedThing), MenuOptionPriority.High), context.FirstSelectedPawn, clickedThing);
        }
        else
        {
            if (count < clickedThing.stackCount)
            {
                yield return new FloatMenuOption("CannotPickUpAll".Translate(clickedThing.Label, clickedThing) + " (" + "CE_InventoryFull".Translate() + ")", null);
            }
            else
            {
                yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("PickUpAll".Translate(clickedThing.Label, clickedThing), () => Pickup_Utility.PickupAll(context.FirstSelectedPawn, clickedThing), MenuOptionPriority.High), context.FirstSelectedPawn, clickedThing);
            }
            yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("PickUpSome".Translate(clickedThing.Label, clickedThing),
                delegate
                {
                    int to = Mathf.Min(count, clickedThing.stackCount);
                    Dialog_Slider window = new Dialog_Slider("PickUpCount".Translate(clickedThing.LabelShort, clickedThing), 1, to, (selectCount) => Pickup_Utility.PickupCount(context.FirstSelectedPawn, clickedThing, selectCount), -2147483648);
                    Find.WindowStack.Add(window);
                    PlayerKnowledgeDatabase.KnowledgeDemonstrated(CE_ConceptDefOf.CE_InventoryWeightBulk, KnowledgeAmount.SpecificInteraction);
                }, MenuOptionPriority.High), context.FirstSelectedPawn, clickedThing);
        }
    }
}
