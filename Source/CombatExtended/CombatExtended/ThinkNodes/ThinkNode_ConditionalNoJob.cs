using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class ThinkNode_ConditionalNoJob : ThinkNode_Conditional
    {
        public override bool Satisfied(Pawn pawn)
        {
            return pawn.CurJob == null;
        }
    }
}
