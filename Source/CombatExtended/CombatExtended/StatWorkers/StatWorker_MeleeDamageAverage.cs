using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended;
public class StatWorker_MeleeDamageAverage : StatWorker_MeleeDamageBase
{

    public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
    {
        var wielder = GetCurrentWielder(req);

        var skilledDamageVariationMin = GetDamageVariationMin(wielder);
        var skilledDamageVariationMax = GetDamageVariationMax(wielder);

        if (req.Def is not ThingDef thingDef)
        {
            return 0;
        }

        float AverageDamageWithVariation(float baseDamage)
        {
            var minDamage = baseDamage * skilledDamageVariationMin;
            var maxDamage = baseDamage * skilledDamageVariationMax;
            return (minDamage + maxDamage) / 2f;
        }

        float averageDamage;
        float averageCooldown;

        if (req.Thing is Pawn pawn)
        {
            var meleeVerbs = pawn.meleeVerbs.GetUpdatedAvailableVerbsList(terrainTools: false);
            averageDamage = meleeVerbs.AverageWeighted(
                verbEntry => verbEntry.GetSelectionWeight(null),
                verbEntry => AverageDamageWithVariation(verbEntry.verb.verbProps.AdjustedMeleeDamageAmount(verbEntry.verb, pawn))
            );
            averageCooldown = meleeVerbs.AverageWeighted(
                verbEntry => verbEntry.GetSelectionWeight(null),
                verbEntry => verbEntry.verb.verbProps.AdjustedCooldown(verbEntry.verb, pawn)
            );

        }
        else
        {
            var verbPropsWithSource = AllMeleeVerbPropsWithSource(thingDef);
            averageDamage = verbPropsWithSource.AverageWeighted(
                vps => AdjustedMeleeSelectionWeight(vps, wielder, req),
                vps => AverageDamageWithVariation(AdjustedMeleeDamageAmount(vps, wielder, req))
            );
            averageCooldown = verbPropsWithSource.AverageWeighted(
                vps => AdjustedMeleeSelectionWeight(vps, wielder, req),
                vps => AdjustedCooldown(vps, wielder, req)
            );
        }

        float dps = averageDamage / averageCooldown;

        if (wielder != null)
        {
            dps *= wielder.GetStatValue(StatDefOf.MeleeHitChance);
        }

        return dps;
    }

    public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
    {
        var wielder = GetCurrentWielder(req);

        var skilledDamageVariationMin = GetDamageVariationMin(wielder);
        var skilledDamageVariationMax = GetDamageVariationMax(wielder);
        var meleeSkillLevel = wielder?.skills?.GetSkill(SkillDefOf.Melee).Level ?? -1;

        if (req.Def is not ThingDef thingDef)
        {
            return base.GetExplanationUnfinalized(req, numberSense);
        }

        var stringBuilder = new StringBuilder();

        if (meleeSkillLevel >= 0)
        {
            stringBuilder.AppendLine("CE_WielderSkillLevel".Translate() + ": " + meleeSkillLevel);
        }
        stringBuilder.AppendLine(string.Format("{0}: {1}% - {2}%",
                                               "CE_DamageVariation".Translate(),
                                               (100 * skilledDamageVariationMin).ToStringByStyle(ToStringStyle.FloatMaxTwo),
                                               (100 * skilledDamageVariationMax).ToStringByStyle(ToStringStyle.FloatMaxTwo)));
        stringBuilder.AppendLine("");

        if (req.Thing is Pawn pawn)
        {
            var meleeVerbs = pawn.meleeVerbs.GetUpdatedAvailableVerbsList(terrainTools: false);
            var cumulativeSelectionWeights = meleeVerbs.Sum(verbEntry => verbEntry.GetSelectionWeight(null));
            foreach (var verbEntry in meleeVerbs)
            {
                var adjustedDamage = verbEntry.verb.verbProps.AdjustedMeleeDamageAmount(verbEntry.verb, pawn);
                var adjustedCooldownTime = verbEntry.verb.verbProps.AdjustedCooldown(verbEntry.verb, pawn);
                var chanceFactor = verbEntry.GetSelectionWeight(null) / cumulativeSelectionWeights;

                if (chanceFactor > 0)
                {
                    ShowExplanationForVerb(
                        stringBuilder,
                        verbEntry.verb.tool,
                        verbEntry.verb.maneuver,
                        skilledDamageVariationMin,
                        skilledDamageVariationMax,
                        adjustedDamage,
                        adjustedCooldownTime,
                        chanceFactor);
                }
            }
        }
        else
        {
            var verbPropsWithSource = AllMeleeVerbPropsWithSource(thingDef);

            var cumulativeSelectionWeights = verbPropsWithSource.Sum(vps => AdjustedMeleeSelectionWeight(vps, wielder, req));

            foreach (var vps in verbPropsWithSource)
            {
                var adjustedDamage = AdjustedMeleeDamageAmount(vps, wielder, req);
                var adjustedCooldownTime = AdjustedCooldown(vps, wielder, req);
                var chanceFactor = AdjustedMeleeSelectionWeight(vps, wielder, req) / cumulativeSelectionWeights;

                ShowExplanationForVerb(
                    stringBuilder,
                    vps.tool,
                    vps.maneuver,
                    skilledDamageVariationMin,
                    skilledDamageVariationMax,
                    adjustedDamage,
                    adjustedCooldownTime,
                    chanceFactor);
            }
        }

        if (wielder != null)
        {
            var hitChanceReq = StatRequest.For(wielder);
            stringBuilder.AppendLine(StatDefOf.MeleeHitChance.Worker.GetExplanationUnfinalized(hitChanceReq, StatDefOf.MeleeHitChance.toStringNumberSense).TrimEndNewlines().Indented());
            stringBuilder.Append(StatDefOf.MeleeHitChance.Worker.GetExplanationFinalizePart(hitChanceReq, StatDefOf.MeleeHitChance.toStringNumberSense, wielder.GetStatValue(StatDefOf.MeleeHitChance)).Indented());
        }

        return stringBuilder.ToString();
    }

    private void ShowExplanationForVerb(
        StringBuilder stringBuilder,
        Tool tool,
        ManeuverDef maneuver,
        float skilledDamageVariationMin,
        float skilledDamageVariationMax,
        float adjustedDamage,
        float adjustedCooldownTime,
        float chanceFactor)
    {
        var minDPS = adjustedDamage / adjustedCooldownTime * skilledDamageVariationMin;
        var maxDPS = adjustedDamage / adjustedCooldownTime * skilledDamageVariationMax;

        var maneuverString = "(" + maneuver + ")";

        stringBuilder.AppendLine("  " + "Tool".Translate() + ": " + tool.ToString() + " " + maneuverString);
        stringBuilder.AppendLine("    " + "CE_DescBaseDamage".Translate() + ": " + tool.power.ToStringByStyle(ToStringStyle.FloatMaxTwo));
        stringBuilder.AppendLine("    " + "CE_AdjustedForWeapon".Translate() + ": " + adjustedDamage.ToStringByStyle(ToStringStyle.FloatMaxTwo));
        stringBuilder.AppendLine("    " + "CooldownTime".Translate() + ": " + adjustedCooldownTime.ToStringByStyle(ToStringStyle.FloatMaxTwo) + " seconds");
        stringBuilder.AppendLine("    " + "CE_DPS".Translate() + ": " + (adjustedDamage / adjustedCooldownTime).ToStringByStyle(ToStringStyle.FloatMaxTwo));
        stringBuilder.AppendLine(string.Format("    " + "CE_DamageVariation".Translate() + ": {0} - {1}",
            minDPS.ToStringByStyle(ToStringStyle.FloatMaxTwo),
            maxDPS.ToStringByStyle(ToStringStyle.FloatMaxTwo)));
        stringBuilder.AppendLine("    " + "CE_FinalAverageDamage".Translate() + ": " + ((minDPS + maxDPS) / 2f).ToStringByStyle(ToStringStyle.FloatMaxTwo));
        stringBuilder.AppendLine("    " + "CE_ChanceFactor".Translate() + ": " + chanceFactor.ToStringByStyle(ToStringStyle.FloatMaxTwo));
        stringBuilder.AppendLine();
    }

}
