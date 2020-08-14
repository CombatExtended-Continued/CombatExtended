using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended.HarmonyCE
{
    /* Overrides the CompReloadable reload job if the pawn has suitable ammo in their inventory.
     * If no inventory ammo is available, base method is allowed to execute (reload from stockpiles).
     */
    [HarmonyPatch(typeof(JobGiver_Reload), "TryGiveJob")]
    internal static class Harmony_JobGiver_Reload_TryGiveJob
    {
        internal static bool Prefix(Pawn pawn, ref Job __result)
        {
            CompReloadable compReloadable = ReloadableUtility.FindSomeReloadableComponent(pawn, false);
            if (compReloadable != null)
            {
                var inventoryAmmo = pawn?.inventory?.innerContainer?.InnerListForReading?.Find(thing => thing.def == compReloadable.AmmoDef);
                if (inventoryAmmo != null)
                {
                    int dropCount = Mathf.Min(compReloadable.MaxAmmoNeeded(true), inventoryAmmo.stackCount);
                    pawn.inventory.innerContainer.TryDrop(inventoryAmmo, pawn.Position, pawn.Map, ThingPlaceMode.Direct, dropCount, out Thing dropThing);
                    var droppedAmmo = new List<Thing>
                    {
                        dropThing
                    };
                    __result = JobGiver_Reload.MakeReloadJob(compReloadable, droppedAmmo);
                    return false;
                }
                
            }
            return true;
        }
    }
}