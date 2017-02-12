using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class StatWorker_CarryWeight : StatWorker
    {
        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            float value = base.GetValueUnfinalized(req, applyPostProcess);
            Pawn p = req.Thing as Pawn;
            if (p != null)
            {
                value += MassBulkUtility.BaseCarryWeight(p);
            }
            return value;
        }
    }
}
