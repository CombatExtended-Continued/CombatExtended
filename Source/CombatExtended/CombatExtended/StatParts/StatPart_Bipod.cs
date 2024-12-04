using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CombatExtended;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;


namespace CombatExtended
{
    public class Bipod_Recoil_StatPart : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (Controller.settings.BipodMechanics)
            {
                if (req.HasThing)
                {
                    var varA = req.Thing.TryGetComp<BipodComp>();
                    if (varA != null)
                    {
                        if (varA.IsSetUpRn)
                        {
                            val *= varA.Props.recoilMulton;
                        }
                        else
                        {
                            val *= varA.Props.recoilMultoff;
                        }
                    }
                }
                else if (req.Def is ThingDef reqDef)
                {
                    var bipodProps = reqDef.GetCompProperties<CompProperties_BipodComp>();
                    val *= bipodProps?.recoilMulton ?? 1f;
                }
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (Controller.settings.BipodMechanics)
            {
                if (req.HasThing)
                {
                    var varA = req.Thing.TryGetComp<BipodComp>();
                    if (varA != null)
                    {
                        if (varA.IsSetUpRn)
                        {
                            return "Bipod IS set up -" + " x".Colorize(ColorLibrary.Green) + varA.Props.recoilMulton.ToString().Colorize(ColorLibrary.Green);
                        }
                        else
                        {
                            return "Bipod is NOT set up -" + " x".Colorize(ColorLibrary.LogError) + varA.Props.recoilMultoff.ToString().Colorize(ColorLibrary.LogError);
                        }
                    }
                }
                else if (req.Def is ThingDef reqDef)
                {
                    var bipodProps = reqDef.GetCompProperties<CompProperties_BipodComp>();
                    if (bipodProps != null) { return "Bipod IS set up -" + " x".Colorize(ColorLibrary.Green) + bipodProps.recoilMulton.ToString().Colorize(ColorLibrary.Green); }
                }
            }
            return null;
        }
    }


    public class Bipod_Sway_StatPart : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (Controller.settings.BipodMechanics)
            {
                if (req.HasThing)
                {
                    var varA = req.Thing.TryGetComp<BipodComp>();
                    if (varA != null)
                    {
                        if (varA.IsSetUpRn)
                        {
                            val *= varA.Props.swayMult;
                        }
                        else
                        {
                            val *= varA.Props.swayPenalty;
                        }
                    }
                }
                else if (req.Def is ThingDef reqDef)
                {
                    var bipodProps = reqDef.GetCompProperties<CompProperties_BipodComp>();
                    val *= bipodProps?.swayMult ?? 1f;
                }
            }

        }

        public override string ExplanationPart(StatRequest req)
        {
            if (Controller.settings.BipodMechanics)
            {
                if (req.HasThing)
                {
                    var varA = req.Thing.TryGetComp<BipodComp>();
                    if (varA != null)
                    {
                        if (varA.IsSetUpRn)
                        {
                            return "Bipod IS set up -" + " x".Colorize(ColorLibrary.Green) + varA.Props.swayMult.ToString().Colorize(ColorLibrary.Green);
                        }
                        else
                        {
                            return "Bipod is NOT set up -" + " x".Colorize(ColorLibrary.LogError) + varA.Props.swayPenalty.ToString().Colorize(ColorLibrary.LogError);
                        }
                    }
                }
                else if (req.Def is ThingDef reqDef)
                {
                    var bipodProps = reqDef.GetCompProperties<CompProperties_BipodComp>();
                    if (bipodProps != null) { return "Bipod IS set up -" + " x".Colorize(ColorLibrary.Green) + bipodProps.swayPenalty.ToString().Colorize(ColorLibrary.Green); }
                }
            }
            return null;
        }
    }
}
