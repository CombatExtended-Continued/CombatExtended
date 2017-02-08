using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public class Designator_PutInInventory : Designator
    {
        public Apparel_Backpack backpack;
        public List<Designation> designations;

        private int numOfContents;

        public Designator_PutInInventory()
        {
            useMouseIcon = true;
            designations = new List<Designation>();
            soundSucceeded = SoundDefOf.DesignateHaul;
            soundDragSustain = SoundDefOf.DesignateDragStandard;
            soundDragChanged = SoundDefOf.DesignateDragStandardChanged;
            activateSound = SoundDef.Named("Click");
        }

        public override int DraggableDimensions { get { return 2; } }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            List<Thing> thingList = loc.GetThingList();

            numOfContents = backpack.wearer.inventory.container.Count;

            int designationsTotalStackCount = 0;
            foreach (Designation designation in designations)
                designationsTotalStackCount += designation.target.Thing.stackCount;

            //No Item space or no stack space
            if (designations.Count + numOfContents >= backpack.MaxItem
                || designationsTotalStackCount + backpack.wearer.inventory.container.TotalStackCount >= backpack.MaxStack)
                return new AcceptanceReport("BackpackIsFull".Translate());


            foreach (Thing thing in thingList)
            {
                if (thing.def.category == ThingCategory.Item && !Find.Reservations.IsReserved(thing, Faction.OfPlayer))
                    return true;
            }
            return new AcceptanceReport("InvalidPutInTarget".Translate());
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            List<Thing> thingList = c.GetThingList();
            foreach (Thing thing in thingList)
                if (thing.def.category == ThingCategory.Item && !Find.Reservations.IsReserved(thing, Faction.OfPlayer))
                    designations.Add(new Designation(thing, DesignationDefOf.Haul));
        }

        protected override void FinalizeDesignationSucceeded()
        {
            Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("PutInInventory"));
            jobNew.maxNumToCarry = 1;
            jobNew.targetB = backpack;
            jobNew.targetQueueA = new List<LocalTargetInfo>();

            while (!designations.NullOrEmpty())
            {
                jobNew.targetQueueA.Add(designations.First().target.Thing);
                designations.RemoveAt(0);
            }
            if (!jobNew.targetQueueA.NullOrEmpty())
                //if (backpack.wearer.drafter.CanTakePlayerJob())
                backpack.wearer.drafter.TakeOrderedJob(jobNew);
            //else
            //    backpack.wearer.drafter.QueueJob(jobNew);
            DesignatorManager.Deselect();
        }

        public override void SelectedUpdate()
        {
            foreach (Designation designation in designations)
                designation.DesignationDraw();
        }
    }
}