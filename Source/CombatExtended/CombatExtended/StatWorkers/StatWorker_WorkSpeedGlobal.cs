using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class StatWorker_WorkSpeedGlobal : StatWorker
    {
        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetExplanationUnfinalized(req, numberSense));
            if (req.HasThing)
            {
                CompInventory comp = req.Thing.TryGetComp<CompInventory>();
                if (comp != null)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine("CE_CarriedBulk".Translate() + ": x" + comp.workSpeedFactor.ToStringPercent());
                }
            }

            return stringBuilder.ToString();
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            float value = base.GetValueUnfinalized(req, applyPostProcess);
            if (req.HasThing)
            {
                CompInventory comp = req.Thing.TryGetComp<CompInventory>();
                if (comp != null)
                {
                    value *= comp.workSpeedFactor;
                }
            }
            return value;
        }
    }
}
