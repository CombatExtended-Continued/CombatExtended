using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class StatWorker_Caliber : StatWorker
    {
        public override bool ShouldShowFor(BuildableDef eDef)
        {
            var thingDef = eDef as ThingDef;
            return thingDef?.GetCompProperties<CompProperties_AmmoUser>()?.ammoSet != null;
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            StringBuilder stringBuilder = new StringBuilder();
            var ammoProps = (req.Def as ThingDef)?.GetCompProperties<CompProperties_AmmoUser>();
            if (ammoProps != null)
            {
                // Append various ammo stats
                stringBuilder.AppendLine(ammoProps.ammoSet.LabelCap + "\n");
                foreach (var cur in ammoProps.ammoSet.ammoTypes)
                {
                    string label = string.IsNullOrEmpty(cur.ammo.ammoClass.LabelCapShort) ? cur.ammo.ammoClass.LabelCap : cur.ammo.ammoClass.LabelCapShort;
                    stringBuilder.AppendLine(label + ":\n" + cur.projectile.GetProjectileReadout());
                }
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }

        public override string GetStatDrawEntryLabel(StatDef stat, float value, ToStringNumberSense numberSense, StatRequest optionalReq)
        {
            return (optionalReq.Def as ThingDef)?.GetCompProperties<CompProperties_AmmoUser>().ammoSet.LabelCap;
        }
    }
}
