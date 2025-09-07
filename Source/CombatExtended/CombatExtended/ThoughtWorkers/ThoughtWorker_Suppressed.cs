using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    class ThoughtWorker_Suppressed : ThoughtWorker
    {
        public override ThoughtState CurrentStateInternal(Pawn p)
        {
            CompSuppressable comp = p.TryGetComp<CompSuppressable>();
            if (comp != null)
            {
                if (comp.IsHunkering)
                {
                    return ThoughtState.ActiveAtStage(2);
                }
                else if (comp.isSuppressed)
                {
                    return ThoughtState.ActiveAtStage(1);
                }
                else if (comp.CurrentSuppression > 0)
                {
                    return ThoughtState.ActiveAtStage(0);
                }
            }
            return ThoughtState.Inactive;
        }
    }
}
