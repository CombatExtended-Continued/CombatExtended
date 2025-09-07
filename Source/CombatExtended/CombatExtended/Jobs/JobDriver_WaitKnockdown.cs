using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace CombatExtended
{
    public class JobDriver_WaitKnockdown : JobDriver_Wait
    {
        public override void SetInitialPosture()
        {
            pawn.jobs.posture = PawnPosture.LayingOnGroundNormal;
        }
    }
}
