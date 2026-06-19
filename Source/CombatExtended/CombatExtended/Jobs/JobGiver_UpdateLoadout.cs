using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended;
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

    private const int TicksThrottleCooldown = 1800;
    private const int ProximitySearchRadius = 20;
    private const int MaximumSearchRadius = 80;
    private const int TicksBeforeDropRaw = 40000;

    #endregion

    #region Fields

    private static Dictionary<int, int> _throttle = new Dictionary<int, int>();

    #endregion

    #region Constructors

    static JobGiver_UpdateLoadout()
    {
        CacheClearComponent.AddClearCacheAction(() => _throttle.Clear());
    }

    #endregion

    #region Methods
    /// <summary>
    /// Gets a priority value of how important it is for a pawn to do pickup/drop activities.
    /// </summary>
    /// <param name="pawn">Pawn to fetch priority for</param>
    /// <returns>float indicating priority of pickup/drop job</returns>
    public override float GetPriority(Pawn pawn)
    {
        CompInventory comp = pawn.TryGetComp<CompInventory>();
        if (comp == null)
        {
            return 0f;
        }
        if (!_throttle.TryGetValue(pawn.thingIDNumber, out int ticks) || GenTicks.TicksGame - ticks > TicksThrottleCooldown)
        {
            return 30f;
        }
        if (pawn.HasExcessThing())
        {
            return 9.2f;
        }
        LoadoutSlot slot = GetPrioritySlot(pawn, out ItemPriority priority, out _, out _, out _);
        if (slot == null)
        {
            return 0f;
        }
        if (priority == ItemPriority.Low)
        {
            return 1f;
        }
        TimeAssignmentDef assignment = (pawn.timetable != null) ? pawn.timetable.CurrentAssignment : TimeAssignmentDefOf.Anything;
        if (assignment == TimeAssignmentDefOf.Sleep)
        {
            return 1f;
        }
        return 2.8f;
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
            if (loadout != null && !loadout.defaultLoadout)
            {
                // Need to generate a dictionary and nibble like when dropping in order to allow for conflicting loadouts to work properly.
                Dictionary<ThingDef, Integer> listing = pawn.GetStorageByThingDef();

                // process each loadout slot... (While the LoadoutSlot.countType property only really makes sense in the context of genericDef != null, it should be the correct value (pickupDrop) on .thingDef != null.)
                foreach (LoadoutSlot curSlot in loadout.GetSlotsFor(pawn).Where(s => s.countType != LoadoutCountType.dropExcess))
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
                            {
                                listing.Remove(curSlot.thingDef);
                            }
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
                            {
                                killKeys.Add(def);
                            }
                            if (wantCount <= 0)
                            {
                                break;
                            }
                        }
                        foreach (ThingDef def in killKeys) // oddly enough removing a dictionary entry while enumerating hasn't caused a problem but this is the 'correct' way based on reading.
                        {
                            listing.Remove(def);
                        }
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
                            {
                                carriedBy = curCarrier;
                            }
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
        // An ordered custom group acquires its members strictly in list order: only fall back to a later
        // member once nothing for an earlier one can be found anywhere reachable.
        if (curSlot.genericDef is ListLoadoutGenericDef { ordered: true } listDef)
        {
            curPriority = ItemPriority.None;
            curThing = null;
            curCarrier = null;
            foreach ((ThingRequest req, Predicate<ThingDef> matches) in OrderedMatchers(listDef, new HashSet<ListLoadoutGenericDef>()))
            {
                FindPickupMatching(pawn, curSlot, findCount, req, matches, out curPriority, out curThing, out curCarrier);
                if (curThing != null)
                {
                    return;
                }
            }
            return;
        }

        ThingRequest defaultReq;
        Predicate<ThingDef> defaultMatch;
        if (curSlot.genericDef != null)
        {
            defaultReq = ThingRequest.ForGroup(curSlot.genericDef.thingRequestGroup);
            defaultMatch = curSlot.genericDef.lambda;
        }
        else
        {
            defaultReq = curSlot.thingDef.Minifiable ? ThingRequest.ForGroup(ThingRequestGroup.MinifiedThing) : ThingRequest.ForDef(curSlot.thingDef);
            defaultMatch = td => td == curSlot.thingDef;
        }
        FindPickupMatching(pawn, curSlot, findCount, defaultReq, defaultMatch, out curPriority, out curThing, out curCarrier);
    }

    // Flattens an ordered group into (request, matcher) pairs in member order. Nested ordered groups are
    // expanded in place so their own order is honored; built-in generics and unordered groups match as a
    // single unit via their union lambda. The visited set guards against malformed cyclic nesting.
    private static IEnumerable<(ThingRequest, Predicate<ThingDef>)> OrderedMatchers(ListLoadoutGenericDef group, HashSet<ListLoadoutGenericDef> visited)
    {
        if (!visited.Add(group))
        {
            yield break;
        }
        foreach (Def member in group.members)
        {
            if (member is ListLoadoutGenericDef { ordered: true } sub)
            {
                foreach ((ThingRequest, Predicate<ThingDef>) matcher in OrderedMatchers(sub, visited))
                {
                    yield return matcher;
                }
            }
            else if (member is LoadoutGenericDef generic)
            {
                yield return (ThingRequest.ForGroup(generic.thingRequestGroup), generic.lambda);
            }
            else if (member is ThingDef thingDef)
            {
                ThingRequest req = thingDef.Minifiable ? ThingRequest.ForGroup(ThingRequestGroup.MinifiedThing) : ThingRequest.ForDef(thingDef);
                yield return (req, td => td == thingDef);
            }
        }
    }

    private static void FindPickupMatching(Pawn pawn, LoadoutSlot curSlot, int findCount, ThingRequest req, Predicate<ThingDef> matches, out ItemPriority curPriority, out Thing curThing, out Pawn curCarrier)
    {
        curPriority = ItemPriority.None;
        curThing = null;
        curCarrier = null;

        Predicate<Thing> isFoodInPrison = (Thing t) => (t.GetRoom()?.IsPrisonCell ?? false) && t.def.IsNutritionGivingIngestible && pawn.Faction.IsPlayer;
        Predicate<Thing> findItem = t => matches(t.GetInnerIfMinified().def);
        Predicate<Thing> search = t => findItem(t) && !t.IsForbidden(pawn) && pawn.CanReserve(t, 10, 1) && !isFoodInPrison(t) && EquipmentUtility.CanEquip(t, pawn) && AllowedByFoodRestriction(t, pawn);

        // look for a thing near the pawn.
        curThing = GenClosest.ClosestThingReachable(
                       pawn.Position,
                       pawn.Map,
                       req,
                       PathEndMode.ClosestTouch,
                       TraverseParms.For(pawn, Danger.None, TraverseMode.ByPawn),
                       ProximitySearchRadius,
                       search);
        if (curThing != null)
        {
            curPriority = ItemPriority.Proximity;
        }
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
                if (!curThing.def.IsNutritionGivingIngestible && (float)findCount / curSlot.count >= 0.5f)
                {
                    curPriority = ItemPriority.LowStock;
                }
                else
                {
                    curPriority = ItemPriority.Low;
                }
            }
        }
    }

    private static bool AllowedByFoodRestriction(Thing thing, Pawn pawn)
    {
        if (thing != null && thing.def.IsNutritionGivingIngestible)
        {
            return pawn.foodRestriction.GetCurrentRespectedRestriction(pawn)?.Allows(thing) ?? true; //better to ignore food restrictions than never pick up a meal
        }
        else
        {
            return true;
        }
    }

    /// <summary>
    /// Gets the unreserved stack count of a Thing.
    /// </summary>
    /// <param name="target">The Thing for which to get the unreserved stack count.</param>
    /// <returns>Returns 0 if the target is not a Thing, is not on a Map, or all of its stack is reserved. Otherwise returns the unreserved stack count of a target.</returns>
    private static int GetUnreservedStackCount(Thing thing)
    {
        var reservations = thing.Map?.reservationManager.ReservationsReadOnly;
        if (reservations == null)
        {
            return 0;
        }

        int stackCount = thing.stackCount;
        for (int i = 0; stackCount > 0 && i < reservations.Count(); ++i)
        {
            var reservation = reservations[i];
            if (reservation.Target.Thing != thing)
            {
                continue;
            }
            stackCount -= reservation.StackCount;
        }
        return stackCount;
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
        if (inventory == null)
        {
            return null;
        }
        if (pawn.equipment?.Primary is WeaponPlatform platform)
        {
            platform.TrySyncPlatformLoadout(pawn);
        }

        Loadout loadout = pawn.GetLoadout();
        if (loadout != null && !loadout.defaultLoadout)
        {
            ThingWithComps dropEq;
            if (pawn.GetExcessEquipment(out dropEq))
            {
                ThingWithComps droppedEq;
                if (pawn.equipment.TryDropEquipment(pawn.equipment.Primary, out droppedEq, pawn.Position, false))
                {
                    if (droppedEq != null)
                    {
                        return HaulAIUtility.HaulToStorageJob(pawn, droppedEq, forced: false);
                    }
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
                        return HaulAIUtility.HaulToStorageJob(pawn, droppedThing, forced: false);
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
                        && (pawn.equipment == null || pawn.equipment.Primary == null || !loadout.GetSlotsFor(pawn).Any(s => s.thingDef == pawn.equipment.Primary.def || (s.genericDef != null && s.countType == LoadoutCountType.pickupDrop
                                && s.genericDef.lambda(pawn.equipment.Primary.def)))))
                {
                    doEquip = true;
                    count = 1;
                }
                // Calculate the unreserved stack count; if all of the stack is reserved, perform no job
                count = Mathf.Min(GetUnreservedStackCount(closestThing), count);
                if (count <= 0)
                {
                    return null;
                }

                if (carriedBy == null)
                {
                    // Equip gun if unarmed or current gun is not in loadout
                    if (doEquip)
                    {
                        return JobMaker.MakeJob(JobDefOf.Equip, closestThing);
                    }
                    Job job = JobMaker.MakeJob(JobDefOf.TakeCountToInventory, closestThing);
                    job.count = count;
                    job.MakeDriver(pawn);
                    return job;
                }
                else
                {
                    Job job = JobMaker.MakeJob(CE_JobDefOf.TakeFromOther, closestThing, carriedBy, doEquip ? pawn : null);
                    job.MakeDriver(pawn);
                    job.count = count;
                    return job;
                }
            }
        }
        return pawn.thinker?.TryGetMainTreeThinkNode<JobGiver_OptimizeApparel>()?.TryGiveJob(pawn);
    }


    public override Job TryGiveJob(Pawn pawn)
    {
        Job job = GetUpdateLoadoutJob(pawn);
        _throttle[pawn.thingIDNumber] = job == null ? GenTicks.TicksGame : (GenTicks.TicksGame - TicksThrottleCooldown - 1);
        return job;
    }

    #endregion
}
