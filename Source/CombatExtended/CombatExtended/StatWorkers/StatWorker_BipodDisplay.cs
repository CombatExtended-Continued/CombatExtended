using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class StatWorker_BipodDisplay : StatWorker
    {
        public BipodComp bipodComp(StatRequest req)
        {
            var result = req.Thing?.TryGetComp<BipodComp>();

            if (result == null)
            {
                req.Thing.def.statBases.RemoveAll(x => x.stat == this.stat);
            }
            return result;
        }

        public CompProperties_BipodComp bipodCompDef(StatRequest req)
        {
            if (!(req.Def is ThingDef))
            {
                return null;
            }

            var result = ((ThingDef)req.Def).comps.Find(x => x is CompProperties_BipodComp);

            if (result == null)
            {
                ((ThingDef)req.Def).statBases.RemoveAll(x => x.stat == this.stat);
            }

            return (CompProperties_BipodComp)result;
        }

        public VerbPropertiesCE verbPropsCE(StatRequest req)
        {
            var result = (VerbPropertiesCE)req.Thing?.TryGetComp<CompEquippable>().PrimaryVerb.verbProps;

            if (result == null)
            {
                req.Thing.def.statBases.RemoveAll(x => x.stat == this.stat);
            }
            return result;
        }

        public VerbPropertiesCE verbPropsDef(StatRequest req)
        {
            if (!(req.Def is ThingDef))
            {
                return null;
            }

            var result = ((ThingDef)req.Def).verbs.Find(x => x is VerbPropertiesCE);

            if (result == null)
            {
                ((ThingDef)req.Def).statBases.RemoveAll(x => x.stat == this.stat);
            }

            return (VerbPropertiesCE)result;
        }

        public override string GetExplanationFinalizePart(StatRequest req, ToStringNumberSense numberSense, float finalVal)
        {
            if (req.HasThing && req.Thing != null)
            {
                var BipodCompProps = bipodComp(req).Props;

                var VerbPropsCE = verbPropsCE(req);

                string result = "Time to set up bipod: " + BipodCompProps.ticksToSetUp + " ticks (" + (BipodCompProps.ticksToSetUp / 30) + "s)" + "\n" + "Stats when set up: ".Colorize(Color.green) + "\n";

                result += "Recoil: " + Math.Round ( (VerbPropsCE.recoilAmount * BipodCompProps.recoilMulton), 2);

                result += "\n";

                result += "Sway: " + Math.Round( (req.Thing.GetStatValue(CE_StatDefOf.SwayFactor) * BipodCompProps.swayMult), 2);

                result += "\n";

                result += "Range: " + (BipodCompProps.additionalrange + VerbPropsCE.range);

                result += "\n";

                result += "Warmup Time: " + (BipodCompProps.warmupMult * VerbPropsCE.warmupTime);

                result += "\n" + "\n";

                result += "Stats when NOT set up: ".Colorize(Color.red) + "\n";

                result += "Recoil: " + Math.Round( (VerbPropsCE.recoilAmount * BipodCompProps.recoilMultoff), 2);

                result += "\n";

                result += "Sway: " + Math.Round( (req.Thing.GetStatValue(CE_StatDefOf.SwayFactor) * BipodCompProps.swayPenalty), 2);

                result += "\n";

                result += "Range: " + (VerbPropsCE.range);

                result += "\n";

                result += "Warmup Time: " + (BipodCompProps.warmupPenalty * VerbPropsCE.warmupTime);

                return result;
            }
            else if (req.Def != null)
            {
                var BipodCompProps = bipodCompDef(req);

                var VerbPropsCE = verbPropsDef(req);

                if (VerbPropsCE == null | BipodCompProps == null)
                {
                    return base.GetExplanationFinalizePart(req, numberSense, finalVal);
                }

                string result = "Time to set up bipod: " + BipodCompProps.ticksToSetUp + " ticks (" + (BipodCompProps.ticksToSetUp / 30) + "s)" + "\n" + "Stats when set up: ".Colorize(Color.green) + "\n";

                result += "Recoil: " + (VerbPropsCE.recoilAmount * BipodCompProps.recoilMulton);

                result += "\n";

                result += "Sway: " + (((ThingDef)(req.Def)).statBases.Find(x => x.stat == CE_StatDefOf.SwayFactor).value * BipodCompProps.swayPenalty);

                result += "\n";

                result += "Range: " + (BipodCompProps.additionalrange + VerbPropsCE.range);

                result += "\n";

                result += "Warmup Time: " + (BipodCompProps.warmupMult * VerbPropsCE.warmupTime);

                result += "\n" + "\n";

                result += "Stats when NOT set up: ".Colorize(Color.red) + "\n";

                result += "Recoil: " + (VerbPropsCE.recoilAmount * BipodCompProps.recoilMultoff);

                result += "\n";

                result += "Sway: " + (((ThingDef)(req.Def)).statBases.Find(x => x.stat == CE_StatDefOf.SwayFactor).value * BipodCompProps.swayPenalty);

                result += "\n";

                result += "Range: " + (VerbPropsCE.range);

                result += "\n";

                result += "Warmup Time: " + (BipodCompProps.warmupPenalty * VerbPropsCE.warmupTime);

                return result;
            }
            else
            {
                return base.GetExplanationFinalizePart(req, numberSense, finalVal);
            }
        }

        public override string ValueToString(float val, bool finalized, ToStringNumberSense numberSense = ToStringNumberSense.Absolute)
        {
            if (finalized)
            {
                return "Hover over";
            }
            else
            {
                return null;
            }
            
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            return base.GetExplanationUnfinalized(req, numberSense);
        }
    }
}
