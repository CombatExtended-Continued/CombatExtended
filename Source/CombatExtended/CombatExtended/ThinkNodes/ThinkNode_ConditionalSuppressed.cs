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
    class ThinkNode_ConditionalSuppressed : ThinkNode_Conditional
    {
        public override bool Satisfied(Pawn pawn)
        {
            CompSuppressable comp = pawn.TryGetComp<CompSuppressable>();
            return comp != null && comp.CanReactToSuppression && comp.isSuppressed;
        }
    }
}
