using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public class JobGiver_ModifyWeapon : JobGiver_UpdateLoadout
    {
        private const int PRIORITY_COOLDOWN = 15000;

        private static Dictionary<int, int> _lastCheckedAt = new Dictionary<int, int>();
        
        public override float GetPriority(Pawn pawn)
        {
            // throttle how often this job is given
            if (true
                && _lastCheckedAt.TryGetValue(pawn.thingIDNumber, out int ticks)
                && GenTicks.TicksGame - ticks < PRIORITY_COOLDOWN)
                return -1f;
            // do common sense checks
            if (false
                || !pawn.RaceProps.Humanlike
                || !pawn.IsColonist
                || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)
                || pawn.WorkTagIsDisabled(WorkTags.Crafting))
                return -1f;
            // if the weapon has been chaned recently 
            if (pawn.equipment?.Primary is WeaponPlatform platform && !platform.ConfigApplied)
            {
                _lastCheckedAt[pawn.thingIDNumber] = GenTicks.TicksGame;
                return 22f;
            }
            return -1f;
        }

        public override Job TryGiveJob(Pawn pawn)
        {
            if (false
                || pawn.equipment == null
                || pawn.equipment.Primary == null
                || !(pawn.equipment.Primary is WeaponPlatform))
                return null;
            WeaponPlatform platform = pawn.equipment?.Primary as WeaponPlatform;
            // abort if the weapon is already configured and doesn't require any changes.
            if (platform.ConfigApplied)
                return null;

            AttachmentDef attachmentDef;
            // get the crafting bench we are going to use for crafting
            Building bench = pawn.Map.listerBuildings.AllBuildingsColonistOfDef(CE_BuildingDefOf.GunsmithingBench)
                                     .FirstOrFallback(b => pawn.CanReach(b, PathEndMode.InteractionCell, Danger.Unspecified, false), null);
            if (bench == null)
                return null;            
            List<ThingCount> chosenIngThings = new List<ThingCount>();
            
            IBillGiver billGiver = bench as IBillGiver;
            // First try removing the stuff that require removal
            if (platform.RemovalList.Count != 0)                            
                attachmentDef = platform.RemovalList.RandomElement();
            // Attempt to crafta new attachment 
            else if (!TryFindTargetAndIngredients(pawn, bench, platform, out attachmentDef, out chosenIngThings))
                return null;
            Job haulOffJob = null;
            Job modifyJob = TryCreateModifyJob(pawn, platform, attachmentDef, bench, billGiver, chosenIngThings, out haulOffJob);
            // if the job used for clearing the workbench is not null return it and enqueue the crafting job
            if(haulOffJob != null)
            {
                pawn.jobs.jobQueue.EnqueueFirst(modifyJob);
                return haulOffJob;
            }
            return modifyJob;
        }

        /// <summary>
        /// Used to create a job with the custom JobCE.
        /// This will return also a hauling job that is used for clearing the target workbench.
        /// </summary>
        /// <param name="pawn">Pawn</param>
        /// <param name="weapon">Weapon</param>
        /// <param name="attachmentDef">The attachment to be affected</param>
        /// <param name="bench">Crafting bench</param>
        /// <param name="billGiver">Crafting bench BillGiver</param>
        /// <param name="chosenIngThings">Ingredients</param>
        /// <param name="haulOffJob">Hauling job for clearing the target workbench</param>
        /// <returns></returns>
        private static Job TryCreateModifyJob(Pawn pawn, WeaponPlatform weapon, AttachmentDef attachmentDef, Thing bench,  IBillGiver billGiver, List<ThingCount> chosenIngThings, out Job haulOffJob)
        {
            haulOffJob = WorkGiverUtility.HaulStuffOffBillGiverJob(pawn, billGiver, null);
            JobCE job = new JobCE();
            job.def = CE_JobDefOf.ModifyWeapon;
            job.targetA = bench;
            job.targetThingDefs.Add(attachmentDef);            
            job.targetQueueA = new List<LocalTargetInfo>() { weapon };
            job.targetQueueB = new List<LocalTargetInfo>(chosenIngThings.Count);
            job.countQueue = new List<int>(chosenIngThings.Count);
            for (int i = 0; i < chosenIngThings.Count; i++)
            {                
                job.targetQueueB.Add(chosenIngThings[i].Thing);
                job.countQueue.Add(chosenIngThings[i].Count);
            }
            job.haulMode = HaulMode.ToCellNonStorage;            
            return job;
        }        


        /// <summary>
        /// Used to find the best attachment to be crafted from the addition list in weapon platforms
        /// </summary>
        /// <param name="pawn">Crafter pawn</param>
        /// <param name="bench">Crafting bench</param>
        /// <param name="platform">Weapon</param>
        /// <param name="attachmentDef">The attachmentDef selected for crafting</param>
        /// <param name="chosenIngThings">The ingredients to be used in crafting</param>
        /// <returns>Wether an attachmentDef was selected while finding all required ingredients</returns>
        private static bool TryFindTargetAndIngredients(Pawn pawn, Building bench, WeaponPlatform platform, out AttachmentDef attachmentDef, out List<ThingCount> chosenIngThings)
        {
            chosenIngThings = new List<ThingCount>();
            attachmentDef = null;
            foreach (AttachmentDef def in platform.AdditionList)
            {
                chosenIngThings.Clear();
                if (TryFindIngredientsFor(def, pawn, bench, chosenIngThings))
                {
                    attachmentDef = def;
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// Used to find the ingredients require for crafting an attachment using region travers. This is inspired by the same code in vanilla class WorkGiver_Bill
        /// </summary>
        /// <param name="attachmentDef">The attachment we want to craft</param>
        /// <param name="pawn">The crafter</param>
        /// <param name="bench">The crafting bench</param>
        /// <param name="chosenIngThings">The list of things to be chosen (should be empty when calling this)</param>
        /// <returns>Wether suitable ingredients are found</returns>
        private static bool TryFindIngredientsFor(AttachmentDef attachmentDef, Pawn pawn, Building bench, List<ThingCount> chosenIngThings)
        {            
            // used to stop the traverse
            int remainingTotalCost = 0;
            // used to cache the remaining cost
            Dictionary<int, int> remainingCost = new Dictionary<int, int>();
            // create counters to emulate cost that will be paid
            foreach (ThingDefCountClass countClass in attachmentDef.costList)
            {
                remainingTotalCost += countClass.count;
                remainingCost[countClass.thingDef.index] = countClass.count;
            }
            // These are taken from vanilla but they do make sense
            TraverseParms traverseParams = TraverseParms.For(pawn);
            Region rootReg = bench.Position.GetRegion(pawn.Map);            
            RegionEntryPredicate entryCondition = (Region from, Region r) => r.Allows(traverseParams, isDestination: false);            
            RegionProcessor regionProcessor = delegate (Region r)
            {
                // get all things in a region that are in our cost list
                List<Thing> list = r.ListerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.HaulableEver));
                for (int i = 0; i < list.Count && remainingTotalCost > 0; i++)
                {                    
                    Thing thing = list[i];
                    if (remainingCost.TryGetValue(thing.def.index, out int count)
                            && count > 0
                            && !chosenIngThings.Any(t => t.Thing.thingIDNumber == thing.thingIDNumber)
                            && !thing.IsForbidden(pawn)
                            && pawn.CanReserve(thing)                            
                            && ReachabilityWithinRegion.ThingFromRegionListerReachable(thing, r, PathEndMode.InteractionCell, pawn))
                    {
                        int n = Math.Min(thing.stackCount, count);
                        chosenIngThings.Add(new ThingCount(thing, n));
                        // update both counter. The stop one and the thingDef one
                        remainingCost[thing.def.index] = count - n;
                        remainingTotalCost -= n;
                    }                    
                }                
                return remainingTotalCost == 0;
            };
            RegionTraverser.BreadthFirstTraverse(rootReg, entryCondition, regionProcessor, 200);
            if(remainingTotalCost != 0)
            {                
                chosenIngThings.Clear();
                return false;
            }
            return true;
        }
    }
}
