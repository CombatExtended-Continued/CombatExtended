using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using Verse;
using RimWorld;
using Verse.AI;
using UnityEngine;

namespace CombatExtended.Harmony
{
    [HarmonyPatch(typeof(WorkGiver_InteractAnimal), "TakeFoodForAnimalInteractJob", new Type[] { typeof(Pawn), typeof(Pawn) })]
    public class Harmony_WorkGiver_InteractAnimal_TakeFoodForAnimalInteractJob
    {
        public static void Postfix(WorkGiver_InteractAnimal __instance, ref Job __result, Pawn pawn, Pawn tamee)
        {
            if (__result != null)
            {
                // Check for inventory space
                int numToCarry = __result.count;
                CompInventory inventory = pawn.TryGetComp<CompInventory>();
                if (inventory != null)
                {
                    int maxCount;
                    if (inventory.CanFitInInventory(__result.targetA.Thing, out maxCount))
                    {
                        __result.count = Mathf.Min(numToCarry, maxCount);
                        pawn.Notify_HoldTrackerJob(__result);
                    }
                    else
                    {
                        Messages.Message("CE_TamerInventoryFull".Translate(), pawn, MessageTypeDefOf.RejectInput);
                        __result = null;
                    }
                }
            }
        }
    }
}
