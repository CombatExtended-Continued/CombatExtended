using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended;
public class StatWorker_BipodDisplay : StatWorker
{
    public CompProperties_BipodComp bipodComp(StatRequest req)
    {
        var result = req.Thing?.TryGetComp<BipodComp>().Props;

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
        var result = req.Thing.TryGetComp<CompEquippable>()?.PrimaryVerb?.verbProps as VerbPropertiesCE;

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
            var BipodCompProps = bipodComp(req);
            var VerbPropsCE = verbPropsCE(req);
            string result = "CE_BipodSetupTime".Translate() + BipodCompProps.ticksToSetUp + " ticks (" + (BipodCompProps.ticksToSetUp / 60) + "s)";

            if (Controller.settings.AutoSetUp)
            {
                result += "\n" + "CE_BipodAutoSetupMode".Translate() + "\n";
                if (BipodCompProps.catDef.useAutoSetMode)
                {
                    result += "- " + BipodCompProps.catDef.autosetMode.ToString() + "\n";
                }
                else
                {
                    result += "- " + AimMode.AimedShot.ToString() + "\n";
                    result += "- " + AimMode.SuppressFire.ToString() + "\n";
                }

            }

            result += "\n" + "CE_BipodStatWhenSetUp".Translate().Colorize(ColorLibrary.Green) + "\n";

            result += CE_StatDefOf.Recoil.label + ": " + Math.Round((VerbPropsCE.recoilAmount * BipodCompProps.recoilMulton), 2);
            result += "\n";

            result += CE_StatDefOf.SwayFactor.label + ": " + Math.Round((req.Thing.def.statBases.Find(x => x.stat == CE_StatDefOf.SwayFactor).value * BipodCompProps.swayMult), 2);
            result += "\n";

            result += "CE_BipodStatRange".Translate() + ": " + (BipodCompProps.additionalrange + VerbPropsCE.range);
            result += "\n";

            result += "CE_BipodStatWarmUp".Translate() + ": " + (BipodCompProps.warmupMult * VerbPropsCE.warmupTime);
            result += "\n" + "\n";

            result += "CE_BipodStatWhenNotSetUp".Translate().Colorize(ColorLibrary.LogError) + "\n";

            result += CE_StatDefOf.Recoil.label + ": " + Math.Round((VerbPropsCE.recoilAmount * BipodCompProps.recoilMultoff), 2);

            result += "\n";

            result += CE_StatDefOf.SwayFactor.label + ": " + Math.Round((req.Thing.def.statBases.Find(x => x.stat == CE_StatDefOf.SwayFactor).value * BipodCompProps.swayPenalty), 2);

            result += "\n";

            result += "CE_BipodStatRange".Translate() + ": " + (VerbPropsCE.range);

            result += "\n";

            result += "CE_BipodStatWarmUp".Translate() + ": " + (BipodCompProps.warmupPenalty * VerbPropsCE.warmupTime);

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

            string result = "CE_BipodSetupTime".Translate() + BipodCompProps.ticksToSetUp + " ticks (" + (BipodCompProps.ticksToSetUp / 60) + "s)" + "\n" + "Stats when set up: ".Colorize(ColorLibrary.Green) + "\n";

            result += CE_StatDefOf.Recoil.label + ": " + Math.Round((VerbPropsCE.recoilAmount * BipodCompProps.recoilMulton), 2);

            result += "\n";

            result += CE_StatDefOf.SwayFactor.label + ": " + Math.Round((((ThingDef)(req.Def)).statBases.Find(x => x.stat == CE_StatDefOf.SwayFactor).value * BipodCompProps.swayMult), 2);

            result += "\n";

            result += "CE_BipodStatRange".Translate() + ": " + (BipodCompProps.additionalrange + VerbPropsCE.range);

            result += "\n";

            result += "CE_BipodStatWarmUp".Translate() + ": " + (BipodCompProps.warmupMult * VerbPropsCE.warmupTime);

            result += "\n" + "\n";

            result += "CE_BipodStatWhenNotSetUp".Translate().Colorize(ColorLibrary.LogError) + "\n";

            result += CE_StatDefOf.Recoil.label + ": " + Math.Round((VerbPropsCE.recoilAmount * BipodCompProps.recoilMultoff), 2);

            result += "\n";

            result += CE_StatDefOf.SwayFactor.label + ": " + Math.Round((((ThingDef)(req.Def)).statBases.Find(x => x.stat == CE_StatDefOf.SwayFactor).value * BipodCompProps.swayPenalty), 2);

            result += "\n";

            result += "CE_BipodStatRange".Translate() + ": " + (VerbPropsCE.range);

            result += "\n";

            result += "CE_BipodStatWarmUp".Translate() + ": " + (BipodCompProps.warmupPenalty * VerbPropsCE.warmupTime);

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
            return "CE_BipodHoverOverStat".Translate();
        }
        else
        {
            return null;
        }

    }

    public override bool ShouldShowFor(StatRequest req)
    {
        return req.Thing?.def.comps.Any(x => x is CompProperties_BipodComp) ?? (req.Def is ThingDef && ((ThingDef)req.Def).comps.Any(x => x is CompProperties_BipodComp));
    }
}
