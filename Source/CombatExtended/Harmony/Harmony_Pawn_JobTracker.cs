using System;
using HarmonyLib;
using Verse.AI;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.TryOpportunisticJob))]
    public static class Harmony_Pawn_JobTracker
    {
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(Pawn_JobTracker __instance, ref Job __result, Job job)
        {

        }
    }
}
