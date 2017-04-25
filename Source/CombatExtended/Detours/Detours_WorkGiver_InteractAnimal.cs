using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace CombatExtended.Detours
{
    internal static class Detours_WorkGiver_InteractAnimal
    {
        internal static Job TakeFoodForAnimalInteractJob(this WorkGiver_InteractAnimal _this, Pawn pawn, Pawn tamee)
        {
            float reqNutrition = JobDriver_InteractAnimal.RequiredNutritionPerFeed(tamee) * 2f * 4f;
            Thing thing = FoodUtility.BestFoodSourceOnMap(pawn, tamee, false, FoodPreferability.RawTasty, false, false, false, false, false, false);

            if (thing == null)
            {
                return null;
            }

            // Check for inventory space
            int numToCarry = Mathf.CeilToInt(reqNutrition / thing.def.ingestible.nutrition);
            CompInventory inventory = pawn.TryGetComp<CompInventory>();
            if (inventory != null)
            {
                int maxCount;
                if (inventory.CanFitInInventory(thing, out maxCount))
                {
                    numToCarry = Mathf.Min(numToCarry, maxCount);
                }
                else
                {
                    Messages.Message("CE_TamerInventoryFull".Translate(), pawn, MessageSound.RejectInput);
                    return null;
                }
            }

            Job job = new Job(JobDefOf.TakeInventory, thing)
            {
                count = numToCarry
            };
            pawn.Notify_HoldTrackerJob(job);
            return job;
        }
    }
}
