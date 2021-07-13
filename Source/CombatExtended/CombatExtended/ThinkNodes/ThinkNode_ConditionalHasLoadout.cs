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
    public class ThinkNode_ConditionalHasLoadout : ThinkNode_Conditional
    {
        public override bool Satisfied(Pawn pawn)
        {
            var loadout = pawn.GetLoadout();
            return loadout != null && !loadout.Slots.NullOrEmpty();
        }
    }
}
