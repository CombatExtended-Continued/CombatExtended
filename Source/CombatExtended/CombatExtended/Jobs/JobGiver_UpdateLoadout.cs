using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public class JobGiver_UpdateLoadout : ThinkNode_JobGiver
    {
        #region InnerClasses
        private enum ItemPriority : byte
        {
            None,
            Low,
            LowStock,
            Proximity
        }
        #endregion

        #region Constants  

        private const int ProximitySearchRadius = 20;
        private const int MaximumSearchRadius = 80;
        private const int TicksBeforeDropRaw = 40000;

        #endregion

        #region Methods
        /// <summary>
        /// Gets a priority value of how important it is for a pawn to do pickup/drop activities.
        /// </summary>
        /// <param name="pawn">Pawn to fetch priority for</param>
        /// <returns>float indicating priority of pickup/drop job</returns>
        public override float GetPriority(Pawn pawn)
        {
            if (pawn.HasExcessThing())
            {
                return 9.2f;
            }
            CompInventory inventory = pawn.TryGetComp<CompInventory>();
            if (inventory == null)
            {
                return 0f;
            }
            ItemPriority priority;
            Thing unused;
            int i;
            Pawn carriedBy;
            LoadoutSlot slot = GetPrioritySlot(pawn, out priority, out unused, out i, out carriedBy);
            if (slot == null)
            {
                return 0f;
            }
            if (priority == ItemPriority.Low)
            {
                return inventory.UpdatingLoadout ? 35f : 3f;
            }
            TimeAssignmentDef assignment = (pawn.timetable != null) ? pawn.timetable.CurrentAssignment : TimeAssignmentDefOf.Anything;
            if (assignment == TimeAssignmentDefOf.Sleep)
            {
                return inventory.UpdatingLoadout ? 35f : 3f;
            }
            return inventory.UpdatingLoadout ? 35.0f : 9.2f;
        }

        /// <summary>
        /// This starts the work of finding something lacking that the pawn should pickup.
        /// </summary>
        /// <param name="pawn">Pawn who's inventory and loadout should be considered.</param>
        /// <param name="priority">Priority value for how important doing this job is.</param>
        /// <param name="closestThing">The thing found to be picked up.</param>
        /// <param name="count">The amount of closestThing to pickup.  Already checked if inventory can hold it.</param>
        /// <param name="carriedBy">If unable to find something on the ground to pickup, the pawn (pack animal/prisoner) which has the item to take.</param>
        /// <returns>LoadoutSlot which has something that can be picked up.</returns>
        private static LoadoutSlot GetPrioritySlot(Pawn pawn, out ItemPriority priority, out Thing closestThing, out int count, out Pawn carriedBy)
        {
            priority = ItemPriority.None;
            LoadoutSlot slot = null;
            closestThing = null;
            count = 0;
            carriedBy = null;

            CompInventory inventory = pawn.TryGetComp<CompInventory>();
            if (inventory != null && inventory.container != null)
            {
                Loadout loadout = pawn.GetLoadout();
                if (loadout != null && !loadout.Slots.NullOrEmpty())
                {
                    // Need to generate a dictionary and nibble like when dropping in order to allow for conflicting loadouts to work properly.
                    Dictionary<ThingDef, Integer> listing = pawn.GetStorageByThingDef();

                    // process each loadout slot... (While the LoadoutSlot.countType property only really makes sense in the context of genericDef != null, it should be the correct value (pickupDrop) on .thingDef != null.)
                    foreach (LoadoutSlot curSlot in loadout.Slots.Where(s => s.countType != LoadoutCountType.dropExcess))
                    {
                        Thing curThing = null;
                        ItemPriority curPriority = ItemPriority.None;
                        Pawn curCarrier = null;
                        int wantCount = curSlot.count;

                        if (curSlot.thingDef != null)
                        {
                            if (listing.ContainsKey(curSlot.thingDef))
                            {
                                int amount = listing[curSlot.thingDef].value >= wantCount ? wantCount : listing[curSlot.thingDef].value;
                                listing[curSlot.thingDef].value -= amount;
                                wantCount -= amount;
                                if (listing[curSlot.thingDef].value <= 0)
                                    listing.Remove(curSlot.thingDef);
                            }
                        }
                        if (curSlot.genericDef != null)
                        {
                            List<ThingDef> killKeys = new List<ThingDef>();
                            int amount;
                            foreach (ThingDef def in listing.Keys.Where(td => curSlot.genericDef.lambda(td)))
                            {
                                amount = listing[def].value >= wantCount ? wantCount : listing[def].value;
                                listing[def].value -= amount;
                                wantCount -= amount;
                                if (listing[def].value <= 0)
                                    killKeys.Add(def);
                                if (wantCount <= 0)
                                    break;
                            }
                            foreach (ThingDef def in killKeys) // oddly enough removing a dictionary entry while enumerating hasn't caused a problem but this is the 'correct' way based on reading.
                                listing.Remove(def);
                        }
                        if (wantCount > 0)
                        {
                            FindPickup(pawn, curSlot, wantCount, out curPriority, out curThing, out curCarrier);

                            if (curPriority > priority && curThing != null && inventory.CanFitInInventory(curThing, out count))
                            {
                                priority = curPriority;
                                slot = curSlot;
                                count = count >= wantCount ? wantCount : count;
                                closestThing = curThing;
                                if (curCarrier != null)
                                    carriedBy = curCarrier;
                            }
                            if (priority >= ItemPriority.LowStock)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            return slot;
        }

        /// <summary>
        /// Used by GetPrioritySlot, actually finds a requested thing.
        /// </summary>
        /// <param name="pawn">Pawn to be considered.  Used in checking equipment and position when looking for nearby things.</param>
        /// <param name="curSlot">Pawn's LoadoutSlot being considered.</param>
        /// <param name="findCount">Amount of Thing of ThingDef to try and pickup.</param>
        /// <param name="curPriority">Priority of the job.</param>
        /// <param name="curThing">Thing found near pawn for potential pickup.</param>
        /// <param name="curCarrier">Pawn that is holding the curThing that 'pawn' wants.</param>
        /// <remarks>Was split off into a sepearate method so the code could be run from multiple places in caller but that is no longer needed.</remarks>
        private static void FindPickup(Pawn pawn, LoadoutSlot curSlot, int findCount, out ItemPriority curPriority, out Thing curThing, out Pawn curCarrier)
        {
            curPriority = ItemPriority.None;
            curThing = null;
            curCarrier = null;

            Predicate<Thing> isFoodInPrison = (Thing t) => (t.GetRoom()?.IsPrisonCell ?? false) && t.def.IsNutritionGivingIngestible && pawn.Faction.IsPlayer;
            // Hint: The following block defines how to find items... pay special attention to the Predicates below.
            ThingRequest req;
            if (curSlot.genericDef != null)
                req = ThingRequest.ForGroup(curSlot.genericDef.thingRequestGroup);
            else
                req = curSlot.thingDef.Minifiable ? ThingRequest.ForGroup(ThingRequestGroup.MinifiedThing) : ThingRequest.ForDef(curSlot.thingDef);
            Predicate<Thing> findItem;
            if (curSlot.genericDef != null)
                findItem = t => curSlot.genericDef.lambda(t.GetInnerIfMinified().def);
            else
                findItem = t => t.GetInnerIfMinified().def == curSlot.thingDef;
            Predicate<Thing> search = t => findItem(t) && !t.IsForbidden(pawn) && pawn.CanReserve(t) && !isFoodInPrison(t) && AllowedByBiocode(t, pawn);

            // look for a thing near the pawn.
            curThing = GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                req,
                PathEndMode.ClosestTouch,
                TraverseParms.For(pawn, Danger.None, TraverseMode.ByPawn),
                ProximitySearchRadius,
                search);
            if (curThing != null) curPriority = ItemPriority.Proximity;
            else
            {
                // look for a thing basically anywhere on the map.
                curThing = GenClosest.ClosestThingReachable(
                    pawn.Position,
                    pawn.Map,
                    req,
                    PathEndMode.ClosestTouch,
                    TraverseParms.For(pawn, Danger.None, TraverseMode.ByPawn),
                    MaximumSearchRadius,
                    search);
                if (curThing == null && pawn.Map != null)
                {
                    // look for a thing inside caravan pack animals and prisoners.  EXCLUDE other colonists to avoid looping state.
                    List<Pawn> carriers = pawn.Map.mapPawns.AllPawns.Where(
                        p => (p.inventory?.innerContainer?.InnerListForReading?.Any() ?? false) && (p.RaceProps.packAnimal && p.Faction == pawn.Faction || p.IsPrisoner && p.HostFaction == pawn.Faction)
                        && pawn.CanReserveAndReach(p, PathEndMode.ClosestTouch, Danger.Deadly, int.MaxValue, 0)).ToList();
                    foreach (Pawn carrier in carriers)
                    {
                        Thing thing = carrier.inventory.innerContainer.FirstOrDefault(t => findItem(t));
                        if (thing != null)
                        {
                            curThing = thing;
                            curCarrier = carrier;
                            break;
                        }
                    }
                }
                if (curThing != null)
                {
                    if (!curThing.def.IsNutritionGivingIngestible && (float)findCount / curSlot.count >= 0.5f) curPriority = ItemPriority.LowStock;
                    else curPriority = ItemPriority.Low;
                }
            }
        }

        private static bool AllowedByBiocode(Thing thing, Pawn pawn)
        {
            CompBiocodable compBiocoded = thing.TryGetComp<CompBiocodable>();
            return (compBiocoded == null || !compBiocoded.Biocoded || compBiocoded.CodedPawn == pawn);
        }

        /// <summary>
        /// Tries to give the pawn a job related to picking up or dropping an item from their inventory.
        /// </summary>
        /// <param name="pawn">Pawn to which the job is given.</param>
        /// <returns>Job that the pawn was instructed to do, be it hauling a dropped Thing or going and getting a Thing.</returns>
        public static Job GetUpdateLoadoutJob(Pawn pawn)
        {
            // Get inventory
            CompInventory inventory = pawn.TryGetComp<CompInventory>();
            if (inventory == null) return null;

            Loadout loadout = pawn.GetLoadout();
            if (loadout != null)
            {
                ThingWithComps dropEq;
                if (pawn.GetExcessEquipment(out dropEq))
                {
                    ThingWithComps droppedEq;
                    if (pawn.equipment.TryDropEquipment(pawn.equipment.Primary, out droppedEq, pawn.Position, false))
                    {
                        if (droppedEq != null)
                            return HaulAIUtility.HaulToStorageJob(pawn, droppedEq);
                        Log.Error(string.Concat(pawn, " tried dropping ", dropEq, " from loadout but resulting thing is null"));
                    }
                }
                Thing dropThing;
                int dropCount;
                if (pawn.GetExcessThing(out dropThing, out dropCount))
                {
                    Thing droppedThing;
                    if (inventory.container.TryDrop(dropThing, pawn.Position, pawn.Map, ThingPlaceMode.Near, dropCount, out droppedThing))
                    {
                        if (droppedThing != null)
                        {
                            return HaulAIUtility.HaulToStorageJob(pawn, droppedThing);
                        }
                        Log.Error(string.Concat(pawn, " tried dropping ", dropThing, " from loadout but resulting thing is null"));
                    }
                }

                // Find missing items
                ItemPriority priority;
                Thing closestThing;
                int count;
                Pawn carriedBy;
                bool doEquip = false;
                GetPrioritySlot(pawn, out priority, out closestThing, out count, out carriedBy);
                // moved logic to detect if should equip vs put in inventory here...
                if (closestThing != null)
                {
                    if (closestThing.TryGetComp<CompEquippable>() != null
                        && !(pawn.story != null && pawn.WorkTagIsDisabled(WorkTags.Violent))
                        && (pawn.health != null && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                        && (!pawn.IsItemQuestLocked(pawn.equipment?.Primary))
                        && (pawn.equipment == null || pawn.equipment.Primary == null || !loadout.Slots.Any(s => s.thingDef == pawn.equipment.Primary.def
                                                                                                           || (s.genericDef != null && s.countType == LoadoutCountType.pickupDrop
                                                                                                               && s.genericDef.lambda(pawn.equipment.Primary.def)))))
                    {
                        doEquip = true;
                    }
                    if (carriedBy == null)
                    {
                        // Equip gun if unarmed or current gun is not in loadout
                        if (doEquip)
                        {
                            return JobMaker.MakeJob(JobDefOf.Equip, closestThing);
                        }
                        Job job = JobMaker.MakeJob(JobDefOf.TakeInventory, closestThing);
                        job.count = Mathf.Min(closestThing.stackCount, count);
                        return job;
                    }
                    else
                    {
                        Job job = JobMaker.MakeJob(CE_JobDefOf.TakeFromOther, closestThing, carriedBy, doEquip ? pawn : null);
                        job.count = doEquip ? 1 : Mathf.Min(closestThing.stackCount, count);
                        return job;
                    }
                }
            }
            return null;
        }

        public override Job TryGiveJob(Pawn pawn)
        {
            Job job = GetUpdateLoadoutJob(pawn);
            CompInventory inventory = pawn.TryGetComp<CompInventory>();
            if (inventory != null)
            {
                inventory.UpdatingLoadout = job != null;
            }
            return job;
        }

        #endregion
    }
}
