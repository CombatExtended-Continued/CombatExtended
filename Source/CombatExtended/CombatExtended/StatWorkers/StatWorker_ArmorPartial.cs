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
                if (req.Thing != null)
                {
                    if (req.Thing.def.HasModExtension<PartialArmorExt>())
                    {
                        var result = "";

                        if ((this.stat?.parts ?? null) != null)
                        {
                            foreach (var part in this.stat.parts)
                            {
                                result += "\n" + part.ExplanationPart(req) + "\n";
                            }
                        }
                        result += "CE_StatWorker_ArmorGeneral".Translate() + finalVal.ToString() + " \n \n" + "CE_StatWorker_ArmorSpecific".Translate();

                        var ext = req.Thing.def.GetModExtension<PartialArmorExt>();
                        ;
                        foreach (ApparelPartialStat partstat in ext.stats)
                        {
                            if (partstat.stat == this.stat)
                            {
                                var value = 0f;
                                if (req.Thing is Apparel)
                                {
                                    value = (float)Math.Round(CE_Utility.PartialStat((Apparel)req.Thing, this.stat, partstat.parts.First()), 2);
                                }
                                else if (req.Thing is Pawn pawn)
                                {
                                    value = (float)Math.Round(pawn.PartialStat(partstat.stat, partstat.parts.First()), 2);
                                }

                                result += "\n";
                                result += partstat.stat.formatString.Replace("{0}", value.ToString()) + " for: ";

                                foreach (BodyPartDef bodypart in partstat.parts)
                                {
                                    result += "\n - ";
                                    result += bodypart.label;
                                }
                            }
                        }
                        return result;
                    }
                }
                else
                {


                    if (req.Def.HasModExtension<PartialArmorExt>())
                    {
                        var result = "";

                        if ((this.stat?.parts ?? null) != null)
                        {
                            foreach (var part in this.stat.parts)
                            {
                                string resultAd = part.ExplanationPart(req);
                                if (resultAd != null && resultAd.Any() && resultAd.Count() > 0)
                                {
                                    result += "\n" + resultAd + "\n";
                                }

                            }
                        }

                        result += "CE_StatWorker_ArmorGeneral".Translate() + finalVal.ToString() + " \n \n" + "CE_StatWorker_ArmorSpecific".Translate();

                        var ext = req.Def.GetModExtension<PartialArmorExt>();
                        ;
                        foreach (ApparelPartialStat partstat in ext.stats)
                        {
                            if (partstat.stat == this.stat)
                            {
                                var value = 0f;

                                if (!partstat.useStatic)
                                {
                                    value = (float)Math.Round(((ThingDef)req.Def).GetStatValueAbstract(this.stat) * partstat.mult);
                                }
                                else if (!((ThingDef)req.Def).stuffCategories.NullOrEmpty() || !((ThingDef)req.Def).stuffProps.categories.NullOrEmpty() || ((ThingDef)req.Def).stuffProps != null)
                                {
                                    value = partstat.mult;
                                }
                                else
                                {
                                    value = partstat.staticValue;
                                }

                                result += "\n";
                                if (!this.stat.defName.ToLower().Contains("stuffpower"))
                                    result += partstat.stat.formatString.Replace("{0}", value.ToString()) + " for: ";
                                else
                                {
                                    result += "Mult: " + value.ToStringPercent() + " for:";
                                }

                               

                                foreach (BodyPartDef bodypart in partstat.parts)
                                {
                                    result += "\n - ";
                                    result += bodypart.label;
                                }
                            }


                        }


                        return result;
                    }
                }

            }
            return base.GetExplanationFinalizePart(req, numberSense, finalVal);
        }

        public override string ValueToString(float val, bool finalized, ToStringNumberSense numberSense = ToStringNumberSense.Absolute)
        {
            if (this.stat.defName == "PartialArmorBody")
            {
                return "Hover over";
            }
            return base.ValueToString(val, finalized, numberSense);
        }

        public override bool ShouldShowFor(StatRequest req)
        {
            if (!(req.Def is ThingDef))
            {
                return false;
            }

            if (((ThingDef)req.Def)?.IsApparel ?? req.Thing?.def?.IsApparel ?? false)
            {
                return this.stat.defName != "PartialArmorBody";
            }
            else if (req.Thing is Pawn)
            {
                if (req.Thing.def.HasModExtension<PartialArmorExt>())
                {
                    return true;
                }
                else
                {
                    return this.stat.defName != "PartialArmorBody";
                }
            }
            return false;

        }
    }
}