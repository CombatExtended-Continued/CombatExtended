using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace CombatExtended;
public class StatWorker_MeleeDamage : StatWorker_MeleeDamageBase
{
    public override string GetStatDrawEntryLabel(StatDef stat, float value, ToStringNumberSense numberSense, StatRequest optionalReq, bool finalized = true)
    {
        return GetFinalDisplayValue(optionalReq);
    }

    public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
    {

        var pawn = GetCurrentWielder(req);

        var skilledDamageVariationMin = GetDamageVariationMin(pawn);
        var skilledDamageVariationMax = GetDamageVariationMax(pawn);
        var meleeSkillLevel = pawn?.skills?.GetSkill(SkillDefOf.Melee).Level ?? -1;

        if (req.Def is not ThingDef thingDef)
        {
            return base.GetExplanationUnfinalized(req, numberSense);
        }

        var stringBuilder = new StringBuilder();

        if (meleeSkillLevel >= 0)
        {
            stringBuilder.AppendLine("CE_WielderSkillLevel".Translate() + ": " + meleeSkillLevel);
        }
        stringBuilder.AppendLine(string.Format("CE_DamageVariation".Translate() + ": {0}% - {1}%",
                                               (100 * skilledDamageVariationMin).ToStringByStyle(ToStringStyle.FloatMaxTwo),
                                               (100 * skilledDamageVariationMax).ToStringByStyle(ToStringStyle.FloatMaxTwo)));
        stringBuilder.AppendLine("");

        foreach (var vps in AllMeleeVerbPropsWithSource(thingDef))
        {
            if (vps.tool is ToolCE toolCE)
            {
                var adjustedToolDamage = AdjustedMeleeDamageAmount(vps, pawn, req);
                var maneuverString = "(" + vps.maneuver + ")";
                stringBuilder.AppendLine("  " + "Tool".Translate() + ": " + toolCE.ToString() + " " + maneuverString);
                stringBuilder.AppendLine("    " + "CE_DescBaseDamage".Translate() + ": " + toolCE.power.ToStringByStyle(ToStringStyle.FloatMaxTwo));
                stringBuilder.AppendLine("    " + "CE_AdjustedForWeapon".Translate() + ": " + adjustedToolDamage.ToStringByStyle(ToStringStyle.FloatMaxTwo));
                stringBuilder.AppendLine(string.Format("    " + "StatsReport_FinalValue".Translate() + ": {0} - {1}",
                                                       (adjustedToolDamage * skilledDamageVariationMin).ToStringByStyle(ToStringStyle.FloatMaxTwo),
                                                       (adjustedToolDamage * skilledDamageVariationMax).ToStringByStyle(ToStringStyle.FloatMaxTwo)));
                stringBuilder.AppendLine();
            }
            else
            {
                if (DebugSettings.godMode)
                {
                    Log.Error($"Trying to get stat MeleeDamage from {req.Def.defName} which has no support for Combat Extended.");
                }
                stringBuilder.AppendLine("CE_UnpatchedWeapon".Translate());
            }
        }
        return stringBuilder.ToString();
    }

    public override string GetExplanationFinalizePart(StatRequest req, ToStringNumberSense numberSense, float finalVal)
    {
        return "StatsReport_FinalValue".Translate() + ": " + GetFinalDisplayValue(req);
    }

    private string GetFinalDisplayValue(StatRequest req)
    {
        var pawn = GetCurrentWielder(req);

        var skilledDamageVariationMin = GetDamageVariationMin(pawn);
        var skilledDamageVariationMax = GetDamageVariationMax(pawn);

        if (req.Def is not ThingDef thingDef)
        {
            return "";
        }

        float lowestDamage = Int32.MaxValue;
        float highestDamage = 0f;
        foreach (var vps in AllMeleeVerbPropsWithSource(thingDef))
        {
            var toolDamage = AdjustedMeleeDamageAmount(vps, pawn, req);
            if (toolDamage > highestDamage)
            {
                highestDamage = toolDamage;
            }
            if (toolDamage < lowestDamage)
            {
                lowestDamage = toolDamage;
            }
        }

        return (lowestDamage * skilledDamageVariationMin).ToStringByStyle(ToStringStyle.FloatMaxTwo)
               + " - "
               + (highestDamage * skilledDamageVariationMax).ToStringByStyle(ToStringStyle.FloatMaxTwo);
    }

}
