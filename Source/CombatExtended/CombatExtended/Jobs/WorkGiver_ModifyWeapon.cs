using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public class WorkGiver_ModifyWeapon : WorkGiver_Scanner
    {
        private const int SCAN_COOLDOWN = 300;
        private const int MAX_SCAN_RADIUS = 150;

        /// <summary>
        /// Return the priority for a potential weapon to modify.
        /// </summary>
        /// <param name="pawn">Worker pawn</param>
        /// <param name="t">Target thing</param>
        /// <returns></returns>
        public override float GetPriority(Pawn pawn, TargetInfo t)
        {
            WeaponPlatform weapon = t.Thing as WeaponPlatform;            
            if (weapon == null)
                return 0f;                       
            if (weapon == pawn.equipment?.Primary)
                return 35f;
            // give it higher priority if the weapon is very close.
            return 15f + 15f * Mathf.Clamp01(1f - pawn.Position.DistanceTo(t.Cell) / MAX_SCAN_RADIUS);
        }

        private static Dictionary<int, int> _throttleByPawn = new Dictionary<int, int>();

        /// <summary>
        /// Return wether we should skip this job for this pawn.
        /// </summary>
        /// <param name="pawn">Pawn</param>
        /// <param name="forced">Forced</param>
        /// <returns>Wether to skip</returns>
        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            // try to throttle the scanning
            if (_throttleByPawn.TryGetValue(pawn.thingIDNumber, out int ticks) && GenTicks.TicksGame - ticks < SCAN_COOLDOWN)
                return true;
            _throttleByPawn[pawn.thingIDNumber] = GenTicks.TicksGame;
            // check race and faction first.            
            if (!pawn.RaceProps.Humanlike)
                return true;            
            if ((!pawn.IsColonist && !pawn.IsSlaveOfColony) || pawn.IsPrisoner)
                return true;
            // check if the pawn map has any gunsmithing benches. 
            if (ShouldSkipMap(pawn.Map))
                return true;
            // skip for pawns incapable of crafting, etc..
            if (pawn.WorkTagIsDisabled(WorkTags.Crafting))
                return true;
            if (!(pawn.health?.capacities?.CapableOf(PawnCapacityDefOf.Manipulation) ?? false))
                return true;
            return base.ShouldSkip(pawn, forced);
        }        

        /// <summary>
        /// Return potential weapon that could need modifications.
        /// </summary>
        /// <param name="pawn">The pawn to perform the work</param>
        /// <returns>Weapons that the pawn could modify</returns>
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {            
            // check the primary weapon for potential modifications
            if (pawn.equipment?.Primary is WeaponPlatform primary && !primary.ConfigApplied)            
                yield return primary;            
            foreach (Thing thing in Utilities.GenClosest.WeaponsInRange(pawn.Position, pawn.Map, MAX_SCAN_RADIUS)
                                                        .Where(t => ShouldYield(t, pawn)))
                yield return thing;

            static bool ShouldYield(Thing thing, Pawn pawn)
            {
                return thing is WeaponPlatform weapon
                    && !weapon.ConfigApplied
                    && !weapon.IsForbidden(pawn.factionInt)
                    && pawn.CanReserve(weapon, 1, 1)
                    && pawn.CanReach(weapon, PathEndMode.OnCell, Danger.Deadly);                    
            }
        }

        /// <summary>
        /// Return a job on the provided thing.
        /// </summary>
        /// <param name="pawn">Pawn</param>
        /// <param name="t">Thing</param>
        /// <param name="forced">Wether the job is forced</param>
        /// <returns>Job</returns>
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return TryGetModifyWeaponJob(pawn, t as WeaponPlatform);
        }

        public static Job TryGetModifyWeaponJob(Pawn pawn, WeaponPlatform platform)
        {
            if (!platform.Spawned && pawn.equipment?.Primary != platform)
                return null;
            if (platform.Spawned && (!pawn.CanReserve(platform, 1, 1) || !pawn.CanReach(platform, PathEndMode.OnCell, Danger.Deadly)))
                return null;
            AttachmentDef attachmentDef;
            // get the crafting bench we are going to use for crafting
            Building bench = pawn.Map.listerBuildings.AllBuildingsColonistOfDef(CE_BuildingDefOf.GunsmithingBench)
                                     .FirstOrFallback(b => pawn.CanReserveAndReach(b, PathEndMode.InteractionCell, Danger.Deadly, 1, 1), null);
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
            if (haulOffJob != null)
            {
                pawn.jobs.jobQueue.EnqueueFirst(modifyJob);
                return haulOffJob;
            }
            return modifyJob;
        }

        /// <summary>
        /// Return wether we should skip this map due to it not having any gunsimthing benches.
        /// </summary>
        /// <param name="map">Map</param>
        /// <returns>Wether any benches exists in this map</returns>
        private static bool ShouldSkipMap(Map map)
        {
            return map != null && map.listerThings.ThingsOfDef(CE_BuildingDefOf.GunsmithingBench).Count == 0;
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
            haulOffJob = WorkGiverUtility.HaulStuffOffBillGiverJob(pawn, billGiver, weapon);
            JobCE job = new JobCE();
            job.def = CE_JobDefOf.ModifyWeapon;
            job.targetA = bench;
            job.targetThingDefs.Add(attachmentDef);            
            job.targetQueueA = new List<LocalTargetInfo>() { weapon };
            job.targetQueueB = new List<LocalTargetInfo>(chosenIngThings.Count);
            job.countQueue = new List<int>(chosenIngThings.Count);
            if (weapon.Spawned && !(bench as IBillGiver).IngredientStackCells.Contains(weapon.Position))
            {
                job.targetQueueB.Add(weapon);
                job.countQueue.Add(1);
            }
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
