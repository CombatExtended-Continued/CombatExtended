using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(JobGiver_PickUpOpportunisticWeapon), "ShouldEquip")]
    internal static class Harmony_JobGiver_PickUpOpportunisticWeapon
    {
        [HarmonyPostfix]
        internal static void Hook(Thing newWep, Pawn pawn, ref bool __result)
        {
            __result = __result && newWep.def != CE_ThingDefOf.Gun_BinocularsRadio;
        }
    }
}