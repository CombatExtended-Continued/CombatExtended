using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace CombatExtended
{
    public class StatWorker_MeleeArmorPenetration : StatWorker_MeleeStats
    {
        public override string GetStatDrawEntryLabel(StatDef stat, float value, ToStringNumberSense numberSense, StatRequest optionalReq, bool finalized = true)
        {
            return GetFinalDisplayValue(optionalReq);
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            var tools = (req.Def as ThingDef)?.tools;

            if (tools.NullOrEmpty())
            {
                return base.GetExplanationUnfinalized(req, numberSense);
            }
            var stringBuilder = new StringBuilder();
            var penetrationFactor = GetPenetrationFactor(req);
            var skillFactor = GetSkillFactor(req);
            stringBuilder.AppendLine("CE_WeaponPenetrationFactor".Translate() + ": " + penetrationFactor.ToStringByStyle(ToStringStyle.PercentZero));
            if (Mathf.Abs(skillFactor - 1f) > 0.001f)
            {
                stringBuilder.AppendLine("CE_WeaponPenetrationSkillFactor".Translate() + ": " + skillFactor.ToStringByStyle(ToStringStyle.PercentZero));
            }

            stringBuilder.AppendLine();

            foreach (ToolCE tool in tools)
            {
                var maneuvers = DefDatabase<ManeuverDef>.AllDefsListForReading.Where(d => tool.capacities.Contains(d.requiredCapacity));
                var maneuverString = "(";
                foreach (var maneuver in maneuvers)
                {
                    maneuverString += maneuver.ToString() + "/";
                }
                maneuverString = maneuverString.TrimmedToLength(maneuverString.Length - 1) + ")";
                stringBuilder.AppendLine("  " + "Tool".Translate() + ": " + tool.ToString() + " " + maneuverString);
                var otherFactors = GetOtherFactors(tool, req).Aggregate(1f, (x, y) => x * y);
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
                    stringBuilder.Append(string.Format(" x {0}", skillFactor));
                }
                if (Mathf.Abs(otherFactors - 1f) > 0.001f)
                {
                    stringBuilder.Append(string.Format(" x {0}", otherFactors));
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
                    stringBuilder.Append(string.Format(" x {0}", skillFactor));
                }
                if (Mathf.Abs(otherFactors - 1f) > 0.001f)
                {
                    stringBuilder.Append(string.Format(" x {0}", otherFactors));
                }
                stringBuilder.AppendLine(string.Format(" = {0} {1}",
                                                       (tool.armorPenetrationBlunt * penetrationFactor * skillFactor * otherFactors).ToStringByStyle(ToStringStyle.FloatMaxTwo),
                                                       "CE_MPa".Translate()));
                stringBuilder.AppendLine();
            }
            return stringBuilder.ToString();
        }

        public override string GetExplanationFinalizePart(StatRequest req, ToStringNumberSense numberSense, float finalVal)
        {
            return "StatsReport_FinalValue".Translate() + ": " + GetFinalDisplayValue(req);
        }

        private string GetFinalDisplayValue(StatRequest optionalReq)
        {
            var tools = (optionalReq.Def as ThingDef)?.tools;
            if (tools.NullOrEmpty())
            {
                return "";
            }
            if (tools.Any(x => !(x is ToolCE)))
            {
                Log.Error($"Trying to get stat MeleeArmorPenetration from {optionalReq.Def.defName} which has no support for Combat Extended.");
                return "";
            }

            float totalSelectionWeight = 0f;
            foreach (Tool tool in tools)
            {
                totalSelectionWeight += tool.chanceFactor;
            }
            float totalAveragePenSharp = 0f;
            float totalAveragePenBlunt = 0f;
            foreach (ToolCE tool in tools)
            {
                var weightFactor = tool.chanceFactor / totalSelectionWeight;
                var otherFactors = GetOtherFactors(tool, optionalReq).Aggregate(1f, (x, y) => x * y);
                totalAveragePenSharp += weightFactor * tool.armorPenetrationSharp * otherFactors;
                totalAveragePenBlunt += weightFactor * tool.armorPenetrationBlunt * otherFactors;
            }
            var penetrationFactor = GetPenetrationFactor(optionalReq);
            var skillFactor = GetSkillFactor(optionalReq);

            return (totalAveragePenSharp * penetrationFactor * skillFactor).ToStringByStyle(ToStringStyle.FloatMaxTwo) + " " + "CE_mmRHA".Translate()
                   + ", "
                   + (totalAveragePenBlunt * penetrationFactor * skillFactor).ToStringByStyle(ToStringStyle.FloatMaxTwo) + " " + "CE_MPa".Translate();
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
        private float GetSkillFactor(StatRequest req)
        {
            var skillFactor = 1f;
            if (req.Thing is Pawn pawn)
            {
                skillFactor += skillFactorPerLevel * (pawn.skills.GetSkill(SkillDefOf.Melee).Level - 1);
            }
            else
            {
                var thingHolder = (req.Thing?.ParentHolder as Pawn_EquipmentTracker)?.pawn;
                if (thingHolder != null)
                {
                    skillFactor += skillFactorPerLevel * (thingHolder.skills.GetSkill(SkillDefOf.Melee).Level - 1);
                }
            }
            return skillFactor;
        }
        private IEnumerable<float> GetOtherFactors(Tool tool, StatRequest req)
        {
            return tool.VerbsProperties.Select(x => Mathf.Pow(x.GetDamageFactorFor(tool, req.Thing as Pawn ?? (req.Thing?.ParentHolder as Pawn_EquipmentTracker)?.pawn, null), powerForOtherFactors));
        }

    }
}
