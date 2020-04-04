using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(JobGiver_DropUnusedInventory), "Drop")]
    public static class Harmony_JobGiver_DropUnusedInventory_Drop
    {
        public static bool Prefix(JobGiver_DropUnusedInventory __instance, Pawn pawn, Thing thing)
        {
            // Remove forced hold from timed out tamer food
            if (thing.def.IsIngestible && !thing.def.IsDrug && thing.def.ingestible.preferability <= FoodPreferability.RawTasty)
            {
                if (pawn.HoldTrackerIsHeld(thing))
                {
                    pawn.HoldTrackerForget(thing);
                }
                return true;
            }
            var loadout = pawn.GetLoadout();
            return !(loadout != null && loadout.SlotCount > 0);
        }
    }
}
