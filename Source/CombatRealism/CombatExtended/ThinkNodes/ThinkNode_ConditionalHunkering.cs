using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    class ThinkNode_ConditionalHunkering : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            CompSuppressable comp = pawn.TryGetComp<CompSuppressable>();
            if (comp == null)
            {
                return false;
            }
            float distToSuppressor = (pawn.Position - comp.suppressorLoc).LengthHorizontal;
            if (distToSuppressor < CompSuppressable.minSuppressionDist)
            {
                return false;
            }
            return comp.isHunkering;
        }
    }
}
