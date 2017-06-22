using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using Harmony;

namespace CombatExtended.Harmony
{
    [HarmonyPatch(typeof(JobGiver_DropUnusedInventory), "Drop")]
    public static class Harmony_JobGiver_DropUnusedInventory_Drop
    {
        public static bool Prefix(JobGiver_DropUnusedInventory __instance, Pawn pawn, Thing thing)
        {
            var loadout = pawn.GetLoadout();
            if (loadout == null)
            {
                return true;
            }
            // Remove forced hold from timed out tamer food
            if (thing.def.IsIngestible && !thing.def.IsDrug && thing.def.ingestible.preferability <= FoodPreferability.RawTasty)
            {
                Utility_HoldTracker.HoldTrackerForget(pawn, thing);
            }
            return false;
        }
    }
}
