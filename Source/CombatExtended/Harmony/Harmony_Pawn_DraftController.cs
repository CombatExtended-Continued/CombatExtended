using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse.AI;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(Pawn_DraftController), nameof(Pawn_DraftController.FireAtWill), MethodType.Setter)]
    class Harmony_Pawn_DraftController_StopAttackJobsOnHoldFire
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn_DraftController __instance, bool ___fireAtWillInt)
        {
            if (!___fireAtWillInt)
            {
                var jobTracker = __instance.pawn.jobs;
                foreach (var queuedJob in jobTracker.jobQueue.ToList())
                {
                    if (queuedJob.job.def == JobDefOf.AttackStatic)
                    {
                        jobTracker.EndCurrentOrQueuedJob(queuedJob.job, JobCondition.Incompletable);
                    }
                }
                if (__instance.pawn.CurJobDef == JobDefOf.AttackStatic)
                {
                    jobTracker.EndCurrentJob(JobCondition.Incompletable, true);
                }
            }
        }
    }
}
