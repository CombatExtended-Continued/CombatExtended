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
            var penMult = req.Thing?.GetStatValue(CE_StatDefOf.MeleePenetrationFactor) ?? 1f;
            stringBuilder.AppendLine("CE_WeaponPenetrationFactor".Translate()+": " + penMult.ToStringByStyle(ToStringStyle.PercentZero));
            stringBuilder.AppendLine();
            foreach (ToolCE tool in tools)
            {
                var maneuvers = DefDatabase<ManeuverDef>.AllDefsListForReading.Where(d => tool.capacities.Contains(d.requiredCapacity));
                var maneuverString = "(";
                foreach(var maneuver in maneuvers)
                {
                    maneuverString += maneuver.ToString() + "/";
                }
                maneuverString = maneuverString.TrimmedToLength(maneuverString.Length - 1) + ")";

                stringBuilder.AppendLine("  "+"Tool".Translate()+": " + tool.ToString() + " " + maneuverString);
                stringBuilder.AppendLine(string.Format("    {0}: {1} x {2} = {3} {4}",
                    "CE_DescSharpPenetration".Translate(),
                    tool.armorPenetrationSharp.ToStringByStyle(ToStringStyle.FloatMaxTwo),
                    penMult.ToStringByStyle(ToStringStyle.FloatMaxThree),
                    (tool.armorPenetrationSharp * penMult).ToStringByStyle(ToStringStyle.FloatMaxTwo),
                    "CE_mmRHA".Translate()));
                stringBuilder.AppendLine(string.Format("    {0}: {1} x {2} = {3} {4}",
                    "CE_DescBluntPenetration".Translate(),
                    tool.armorPenetrationBlunt.ToStringByStyle(ToStringStyle.FloatMaxTwo),
                    penMult.ToStringByStyle(ToStringStyle.FloatMaxThree),
                    (tool.armorPenetrationBlunt * penMult).ToStringByStyle(ToStringStyle.FloatMaxTwo),
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
                totalAveragePenSharp += weightFactor * tool.armorPenetrationSharp;
                totalAveragePenBlunt += weightFactor * tool.armorPenetrationBlunt;
            }
            var penMult = optionalReq.Thing?.GetStatValue(CE_StatDefOf.MeleePenetrationFactor) ?? 1f;

            return (totalAveragePenSharp * penMult).ToStringByStyle(ToStringStyle.FloatMaxTwo) + " " + "CE_mmRHA".Translate()
                + ", "
                + (totalAveragePenBlunt * penMult).ToStringByStyle(ToStringStyle.FloatMaxTwo) + " " + "CE_MPa".Translate();
        }

    }
}