using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended;
public class ThinkNode_ConditionalHasSquad : ThinkNode_Conditional
{
    protected override bool Satisfied(Pawn pawn)
    {
        CompSquadBrain comp = pawn.TryGetComp<CompSquadBrain>();
        return comp != null && comp.squad != null;
    }
}
