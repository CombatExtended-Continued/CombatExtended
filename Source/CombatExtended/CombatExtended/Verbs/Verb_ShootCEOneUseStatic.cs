using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public class Verb_ShootCEOneUseStatic : Verb_ShootCEOneUse
    {
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            Job job = JobMaker.MakeJob(JobDefOf.UseVerbOnThingStaticReserve, target);
            job.verbToUse = this;
            this.CasterPawn.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
        }
    }
}
