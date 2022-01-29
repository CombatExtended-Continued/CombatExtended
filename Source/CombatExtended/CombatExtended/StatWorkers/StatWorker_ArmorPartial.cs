using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class StatWorker_ArmorPartial : StatWorker
    {
        public override string GetExplanationFinalizePart(StatRequest req, ToStringNumberSense numberSense, float finalVal)
        {
            if (req.Thing.def.HasModExtension<PartialArmorExt>())
            {
                var result = "CE_StatWorker_ArmorGeneral".Translate() + finalVal.ToString() + " \n \n" + "CE_StatWorker_ArmorSpecific".Translate();
                var ext = req.Thing.def.GetModExtension<PartialArmorExt>();

                foreach(ApparelPartialStat partstat in ext.stats)
                {
                    if (partstat.stat != this.stat)
                    {
                        return base.GetExplanationFinalizePart(req, numberSense, finalVal);
                    }

                    var value = CE_Utility.PartialStat((Apparel)req.Thing, this.stat, partstat.parts.First());

                    result += "\n";
                    result +=  this.stat.formatString.Replace("{0}", value.ToString()) + " for: ";

                    foreach (BodyPartDef bodypart in partstat.parts)
                    {
                        result += "\n";
                        result += bodypart.label;
                    }
                }
                return result;
            }

            return base.GetExplanationFinalizePart(req, numberSense, finalVal);
        }
    }
}
