using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE
{    
    public static class Harmony_JobGiver_AITrash
    {
        [HarmonyPatch(typeof(JobGiver_AITrashColonyClose), nameof(JobGiver_AITrashColonyClose.TryGiveJob))]
        public static class Harmony_AITrashColonyClose_TryGiveJob
        {
            public static bool Prefix(Pawn pawn)
            {                
                return false;
            }
        }

        [HarmonyPatch(typeof(JobGiver_AITrashDutyFocus), nameof(JobGiver_AITrashDutyFocus.TryGiveJob))]
        public static class Harmony_JobGiver_AITrashDutyFocus_TryGiveJob
        {
            public static bool Prefix(Pawn pawn)
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(JobGiver_AITrashBuildingsDistant), nameof(JobGiver_AITrashBuildingsDistant.TryGiveJob))]
        public static class Harmony_JobGiver_AITrashBuildingsDistant_TryGiveJob
        {
            public static bool Prefix(Pawn pawn)
            {
                return false;
            }
        }
    }
}

