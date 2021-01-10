using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended.HarmonyCE
{
    /* Intercepts the outfit manager's attempts to equip a shield or shield belt.
     * If the current loadout has a projectile weapon in it, does not let the outfit manager grab a shield belt.
     * If the current loadout has a shield in it, does not let the outfit manager grab a shield.
     */
    [HarmonyPatch(typeof(JobGiver_OptimizeApparel), "ApparelScoreGain_NewTmp")]
    internal static class Harmony_JobGiver_OptimizeApparel_ApparelScoreGain_NewTmp
    {
        internal static bool Prefix(Pawn pawn, Apparel ap, ref float __result)
        {
            if (ap is ShieldBelt && pawn.GetLoadout().Slots.FirstOrDefault(s => s.count >= 1 && (s.thingDef.IsWeaponUsingProjectiles)) != null)
            {
                __result = -1000f;
                return false;
            }
            if (ap is Apparel_Shield && pawn.GetLoadout().containsShield)
            {
                __result = -1000f;
                return false;
            }
            return true;
        }
    }
}