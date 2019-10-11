using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class StatWorker_MeleeArmorPenetration : StatWorker_MeleeStats
    {
        public override string GetStatDrawEntryLabel(StatDef stat, float value, ToStringNumberSense numberSense, StatRequest optionalReq)
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
            float totalAveragePenRHA = 0f;
            float totalAveragePenKPA = 0f;
            foreach (ToolCE tool in tools)
            {
                var weightFactor = tool.chanceFactor / totalSelectionWeight;
                totalAveragePenRHA += weightFactor * tool.armorPenetrationRHA;
                totalAveragePenKPA += weightFactor * tool.armorPenetrationKPA;
            }
            var penMult = optionalReq.Thing?.GetStatValue(CE_StatDefOf.MeleePenetrationFactor) ?? 1f;

            return (totalAveragePenRHA * penMult).ToStringByStyle(ToStringStyle.FloatMaxTwo) + "mm RHA"
                + ", "
                + (totalAveragePenKPA * penMult).ToStringByStyle(ToStringStyle.Integer) + " kPa";
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
            stringBuilder.AppendLine("Weapon penetration factor: " + penMult.ToStringByStyle(ToStringStyle.PercentZero));
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

                stringBuilder.AppendLine("  Tool: " + tool.ToString() + " " + maneuverString);
                stringBuilder.AppendLine(string.Format("    Sharp penetration: {0} x {1} = {2} mm RHA",
                    tool.armorPenetrationRHA.ToStringByStyle(ToStringStyle.FloatMaxTwo),
                    penMult.ToStringByStyle(ToStringStyle.FloatMaxThree),
                    (tool.armorPenetrationRHA * penMult).ToStringByStyle(ToStringStyle.FloatMaxTwo)));
                stringBuilder.AppendLine(string.Format("    Blunt penetration: {0} x {1} = {2} kPa",
                    tool.armorPenetrationKPA.ToStringByStyle(ToStringStyle.FloatMaxTwo),
                    penMult.ToStringByStyle(ToStringStyle.FloatMaxThree),
                    (tool.armorPenetrationKPA * penMult).ToStringByStyle(ToStringStyle.FloatMaxTwo)));
                stringBuilder.AppendLine();
            }
            return stringBuilder.ToString();
        }

    }
}
