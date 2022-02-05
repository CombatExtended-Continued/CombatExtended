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
            if (Controller.settings.PartialStat)
            {
                if (req.Thing.def.HasModExtension<PartialArmorExt>())
                {
                    var result = "CE_StatWorker_ArmorGeneral".Translate() + finalVal.ToString() + " \n \n" + "CE_StatWorker_ArmorSpecific".Translate();
                    var ext = req.Thing.def.GetModExtension<PartialArmorExt>();
;
                    foreach (ApparelPartialStat partstat in ext.stats)
                    {
                        if (req.Thing.def.IsApparel)
                        {
                            if (partstat.stat != this.stat)
                            {
                                return base.GetExplanationFinalizePart(req, numberSense, finalVal);
                            }
                        }
                        

                        var value = 0f;
                        if (req.Thing is Apparel)
                        {
                            value = CE_Utility.PartialStat((Apparel)req.Thing, this.stat, partstat.parts.First());
                        }
                        else if (req.Thing is Pawn pawn)
                        {
                            value = pawn.PartialStat(partstat.stat, partstat.parts.First());
                        }

                        result += "\n";
                        result += partstat.stat.formatString.Replace("{0}", value.ToString()) + " for: ";

                        foreach (BodyPartDef bodypart in partstat.parts)
                        {
                            result += "\n";
                            result += bodypart.label;
                        }
                    }
                    return result;
                }
            }
            return base.GetExplanationFinalizePart(req, numberSense, finalVal);
        }

        public override string ValueToString(float val, bool finalized, ToStringNumberSense numberSense = ToStringNumberSense.Absolute)
        {
            if (this.stat.defName == "PartialArmorBody")
            {
                return "hover over";
            }
            return base.ValueToString(val, finalized, numberSense);
        }
    }
}
