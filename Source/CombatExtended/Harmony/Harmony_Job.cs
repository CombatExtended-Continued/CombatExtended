using System;
using HarmonyLib;
using Verse.AI;

namespace CombatExtended.HarmonyCE
{
    /*
     * This patch is used to allow us to attach more data to jobs
     * 
     * - For now it allow us to attach defs to jobs as target
     */
    [HarmonyPatch(typeof(Job), nameof(Job.ExposeData))]
    public static class Harmony_Job_ExposeData
    {       
        public static void Postfix(Job __instance)
        {
            if (__instance is JobCE job)
                job.PostExposeData();
        }
    }
}
