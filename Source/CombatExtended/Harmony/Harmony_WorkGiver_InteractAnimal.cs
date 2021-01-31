using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using RimWorld;
using Verse.AI;
using UnityEngine;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(WorkGiver_InteractAnimal), "TakeFoodForAnimalInteractJob", new Type[] { typeof(Pawn), typeof(Pawn) })]
    public class Harmony_WorkGiver_InteractAnimal_TakeFoodForAnimalInteractJob
    {
        public static bool rejectedInventoryMass = false;

        public static void Postfix(WorkGiver_InteractAnimal __instance, ref Job __result, Pawn pawn, Pawn tamee)
        {
            if (__result != null)
            {
                // Check for inventory space
                int numToCarry = __result.count;
                CompInventory inventory = pawn.TryGetComp<CompInventory>();

                float minNutrition = JobDriver_InteractAnimal.RequiredNutritionPerFeed(tamee);
                int requiredThingCount = Mathf.CeilToInt(minNutrition / FoodUtility.GetNutrition(__result.targetA.Thing, FoodUtility.GetFinalIngestibleDef(__result.targetA.Thing)));
                
                if (inventory != null)
                {
                    if (inventory.CanFitInInventory(__result.targetA.Thing, out int maxCount) && requiredThingCount <= maxCount)
                    {
                        __result.count = Mathf.Min(numToCarry, maxCount);
                        pawn.Notify_HoldTrackerItem(__result.targetA.Thing, __result.count);
                    }
                    else
                    {
                        rejectedInventoryMass = true;
                        __result = null;
                    }
                }
            }
        }
    }

    [HarmonyPatch]
    public static class Harmony_MessageFailPatch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(WorkGiver_Tame), nameof(WorkGiver_Tame.JobOnThing));
            yield return AccessTools.Method(typeof(WorkGiver_Train), nameof(WorkGiver_Train.JobOnThing));
        }

        public static void Postfix()
        {
            if (Harmony_WorkGiver_InteractAnimal_TakeFoodForAnimalInteractJob.rejectedInventoryMass)
            {
                JobFailReason.Is("CE_InventoryFull_TameFail".TranslateSimple());
                Harmony_WorkGiver_InteractAnimal_TakeFoodForAnimalInteractJob.rejectedInventoryMass = false;
            }

        }

    }
}