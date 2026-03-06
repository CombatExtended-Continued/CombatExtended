using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace CombatExtended;
public class JobGiver_SquadDuty : ThinkNode_JobGiver
{
    protected override Job TryGiveJob(Pawn pawn)
    {
        CompSquadBrain comp = pawn.TryGetComp<CompSquadBrain>();
        if (comp == null)
        {
            Log.Error("CE tried running JobGiver_SquadDuty on " + pawn.ToString() + " without CompSquadBrain");
            return null;
        }
        Job job = comp.squad.GetJobFor(pawn);
        //if (job == null) job = new Job(JobDefOf.WaitCombat, pawn.Position);
        return job;
    }
}
