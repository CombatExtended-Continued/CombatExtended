using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace CombatExtended;
public class StatWorker_MeleeArmorPenetration : StatWorker_MeleeStats
{

    public override string GetStatDrawEntryLabel(StatDef stat, float value, ToStringNumberSense numberSense, StatRequest optionalReq, bool finalized = true)
    {
        return GetFinalDisplayValue(optionalReq);
    }

    public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
    {
        if (req.Def is not ThingDef thingDef)
        {
            return base.GetExplanationUnfinalized(req, numberSense);
        }

        var pawn = GetCurrentWielder(req);

        var skillFactor = GetSkillFactor(pawn);
        var otherFactors = GetOtherFactors(pawn);

        var stringBuilder = new StringBuilder();

        if (Mathf.Abs(skillFactor - 1f) > 0.001f)
        {
            stringBuilder.AppendLine("CE_WeaponPenetrationSkillFactor".Translate() + ": " + skillFactor.ToStringByStyle(ToStringStyle.PercentZero));
        }

        stringBuilder.AppendLine();

        if (req.Thing is Pawn)
        {
            var meleeVerbs = pawn.meleeVerbs.GetUpdatedAvailableVerbsList(terrainTools: false);
            var cumulativeWeights = meleeVerbs.Sum(verbEntry => verbEntry.GetSelectionWeight(null));
            foreach (var verbEntry in meleeVerbs)
            {
                var penetrationFactor =
                    verbEntry.verb.EquipmentSource?.GetStatValue(CE_StatDefOf.MeleePenetrationFactor) ?? 1f;
                var chance = verbEntry.GetSelectionWeight(null) / cumulativeWeights;

                if (chance > 0)
                {
                    ShowExplanationForVerb(
                        stringBuilder,
                        verbEntry.verb.tool,
                        verbEntry.verb.maneuver,
                        skillFactor,
                        otherFactors,
                        penetrationFactor,
                        chance
                    );
                }
            }
        }
        else
        {
            var penetrationFactor = GetPenetrationFactor(req);
            var meleeVerbPropsWithSource = AllMeleeVerbPropsWithSource(thingDef);
            var cumulativeWeights = meleeVerbPropsWithSource.Sum(vps => AdjustedMeleeSelectionWeight(vps, pawn, req));
            foreach (var vps in meleeVerbPropsWithSource)
            {
                var chance = AdjustedMeleeSelectionWeight(vps, pawn, req) / cumulativeWeights;
                ShowExplanationForVerb(
                    stringBuilder,
                    vps.tool,
                    vps.maneuver,
                    skillFactor,
                    otherFactors,
                    penetrationFactor,
                    chance
                );
            }
        }

        return stringBuilder.ToString();
    }

    private void ShowExplanationForVerb(StringBuilder stringBuilder, Tool verbTool, ManeuverDef maneuver,
        float skillFactor,
        float otherFactors,
        float penetrationFactor,
        float chance)
    {
        if (verbTool is not ToolCE tool)
        {
            return;
        }

        var maneuverString = "(" + maneuver + ")";
        stringBuilder.AppendLine("  " + "Tool".Translate() + ": " + tool.ToString() + " " + maneuverString);


        stringBuilder.AppendLine("   " + "CE_WeaponPenetrationFactor".Translate() + ": " + penetrationFactor.ToStringByStyle(ToStringStyle.PercentZero));

        if (Mathf.Abs(otherFactors - 1f) > 0.001f)
        {
            stringBuilder.AppendLine("   " + "CE_WeaponPenetrationOtherFactors".Translate() + ": " + otherFactors.ToStringByStyle(ToStringStyle.PercentZero));
        }

        stringBuilder.Append(string.Format("    {0}: {1} x {2}",
                                               "CE_DescSharpPenetration".Translate(),
                                               tool.armorPenetrationSharp.ToStringByStyle(ToStringStyle.FloatMaxTwo),
                                               penetrationFactor.ToStringByStyle(ToStringStyle.FloatMaxThree)));
        if (Mathf.Abs(skillFactor - 1f) > 0.001f)
        {
            stringBuilder.Append(string.Format(" x {0}", skillFactor.ToStringByStyle(ToStringStyle.FloatMaxTwo)));
        }
        if (Mathf.Abs(otherFactors - 1f) > 0.001f)
        {
            stringBuilder.Append(string.Format(" x {0}", otherFactors.ToStringByStyle(ToStringStyle.FloatMaxTwo)));
        }
        stringBuilder.AppendLine(string.Format(" = {0} {1}",
                                                (tool.armorPenetrationSharp * penetrationFactor * skillFactor * otherFactors).ToStringByStyle(ToStringStyle.FloatMaxTwo),
                                                "CE_mmRHA".Translate()));


        stringBuilder.Append(string.Format("    {0}: {1} x {2}",
                                               "CE_DescBluntPenetration".Translate(),
                                               tool.armorPenetrationBlunt.ToStringByStyle(ToStringStyle.FloatMaxTwo),
                                               penetrationFactor.ToStringByStyle(ToStringStyle.FloatMaxThree)));
        if (Mathf.Abs(skillFactor - 1f) > 0.001f)
        {
            stringBuilder.Append(string.Format(" x {0}", skillFactor.ToStringByStyle(ToStringStyle.FloatMaxTwo)));
        }
        if (Mathf.Abs(otherFactors - 1f) > 0.001f)
        {
            stringBuilder.Append(string.Format(" x {0}", otherFactors.ToStringByStyle(ToStringStyle.FloatMaxTwo)));
        }
        stringBuilder.AppendLine(string.Format(" = {0} {1}",
                                               (tool.armorPenetrationBlunt * penetrationFactor * skillFactor * otherFactors).ToStringByStyle(ToStringStyle.FloatMaxTwo),
                                               "CE_MPa".Translate()));
        stringBuilder.AppendLine("    " + "CE_ChanceFactor".Translate() + ": " + chance.ToStringByStyle(ToStringStyle.FloatMaxTwo));
        stringBuilder.AppendLine();
    }

    public override string GetExplanationFinalizePart(StatRequest req, ToStringNumberSense numberSense, float finalVal)
    {
        return "StatsReport_FinalValue".Translate() + ": " + GetFinalDisplayValue(req);
    }

    private string GetFinalDisplayValue(StatRequest req)
    {
        if (req.Def is not ThingDef thingDef)
        {
            return "";
        }

        var pawn = GetCurrentWielder(req);
        var otherFactors = GetOtherFactors(pawn);
        var skillFactor = GetSkillFactor(pawn);

        float totalAveragePenSharp;
        float totalAveragePenBlunt;

        if (req.Thing is Pawn)
        {
            // When computing average armor penetration for a pawn, consider the weighted average of the armor penetration of all their available melee verbs.
            // The penetration factor in this case depends on the weapon that provides a given verb.
            var meleeVerbs = pawn.meleeVerbs.GetUpdatedAvailableVerbsList(terrainTools: false);

            totalAveragePenSharp = meleeVerbs.AverageWeighted(
                verbEntry => verbEntry.GetSelectionWeight(null),
                verbEntry => verbEntry.verb.tool is ToolCE tool ? tool.armorPenetrationSharp * otherFactors * verbEntry.verb.EquipmentSource?.GetStatValue(CE_StatDefOf.MeleePenetrationFactor) ?? 1f : 0f
                );
            totalAveragePenBlunt = meleeVerbs.AverageWeighted(
                verbEntry => verbEntry.GetSelectionWeight(null),
                verbEntry => verbEntry.verb.tool is ToolCE tool ? tool.armorPenetrationBlunt * otherFactors * verbEntry.verb.EquipmentSource?.GetStatValue(CE_StatDefOf.MeleePenetrationFactor) ?? 1f : 0f
            );
        }
        else
        {
            // Otherwise, when calculating average armor penetration for a single weapon (or def), be it wielded or unwielded,
            // consider the weighted average of each verb and derive the penetration factor from the weapon/def.
            var verbPropsWithSource = AllMeleeVerbPropsWithSource(thingDef);
            totalAveragePenSharp = verbPropsWithSource.AverageWeighted(
                vps => AdjustedMeleeSelectionWeight(vps, pawn, req),
                vps => vps.tool is ToolCE tool ? tool.armorPenetrationSharp * otherFactors : 0f
            );
            totalAveragePenBlunt = verbPropsWithSource.AverageWeighted(
                vps => AdjustedMeleeSelectionWeight(vps, pawn, req),
                vps => vps.tool is ToolCE tool ? tool.armorPenetrationBlunt * otherFactors : 0f
            );

            var penetrationFactor = GetPenetrationFactor(req);

            totalAveragePenSharp *= penetrationFactor;
            totalAveragePenBlunt *= penetrationFactor;
        }

        return (totalAveragePenSharp * skillFactor).ToStringByStyle(ToStringStyle.FloatMaxTwo) + " " + "CE_mmRHA".Translate()
               + ", "
               + (totalAveragePenBlunt * skillFactor).ToStringByStyle(ToStringStyle.FloatMaxTwo) + " " + "CE_MPa".Translate();
    }

    private float GetPenetrationFactor(StatRequest req)
    {
        var penetrationFactor = 1f;
        if (req.Thing != null)
        {
            penetrationFactor = req.Thing.GetStatValue(CE_StatDefOf.MeleePenetrationFactor);
        }
        else
        {
            //Try to get melee penetration factor/offsets from stuff props if available
            penetrationFactor *= req.StuffDef?.stuffProps?.statFactors?.FirstOrDefault(t => t.stat == CE_StatDefOf.MeleePenetrationFactor)?.value ?? 1f;
            penetrationFactor += req.StuffDef?.stuffProps?.statOffsets?.FirstOrDefault(t => t.stat == CE_StatDefOf.MeleePenetrationFactor)?.value ?? 0f;
        }
        return penetrationFactor;
    }
    public const float skillFactorPerLevel = (25f / 19f) / 100f;
    public const float powerForOtherFactors = 0.75f;
    private float GetSkillFactor(Pawn pawn)
    {
        var skillFactor = 1f;
        if (pawn?.skills != null)
        {
            skillFactor += skillFactorPerLevel * (pawn.skills.GetSkill(SkillDefOf.Melee).Level - 1);
        }
        return skillFactor;
    }
    private float GetOtherFactors(Pawn pawn)
    {
        if (pawn != null)
        {
            return Mathf.Pow(pawn.ageTracker.CurLifeStage.meleeDamageFactor, powerForOtherFactors) *
                   Mathf.Pow(pawn.GetStatValue(StatDefOf.MeleeDamageFactor, true, -1), powerForOtherFactors);
        }

        return 1f;
    }
}
