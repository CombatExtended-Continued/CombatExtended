using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CombatExtended.Compatibility;
using Verse.AI;

namespace CombatExtended.HarmonyCE;
[HarmonyPatch(typeof(Pawn_DraftController), nameof(Pawn_DraftController.FireAtWill), MethodType.Setter)]
class Harmony_Pawn_DraftController_StopAttackJobsOnHoldFire
{
    [HarmonyPostfix]
    public static void Postfix(Pawn_DraftController __instance, bool ___fireAtWillInt)
    {
        if (!___fireAtWillInt)
        {
            // In Multiplayer, the FireAtWill setter is marked as a sync method.
            // Due to how those work - the method will be stopped from running, the call synchronized to all players and then will run
            // fully. However, because of how Harmony works all prefixes, postfixes, finalizers will still run - which will cause issues
            // as this postfix will run before it ends up being synchronized in Multiplayer.
            // Once Multiplayer API exposes `InInterface` method, it should replace this check (as it'll better handle a few edge cases).
            if (global::CombatExtended.Compatibility.Multiplayer.InMultiplayer && !global::CombatExtended.Compatibility.Multiplayer.IsExecutingCommands)
            {
                return;
            }

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
