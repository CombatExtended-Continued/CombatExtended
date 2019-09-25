using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class StatWorker_MeleeDamage : StatWorker_MeleeStats
    {
        private readonly float damageDeviationMin = 0.5f;
        private readonly float damageDeviationMax = 1.5f;


        public override string GetStatDrawEntryLabel(StatDef stat, float value, ToStringNumberSense numberSense, StatRequest optionalReq)
        {
            var tools = (optionalReq.Def as ThingDef)?.tools;
            if (tools.NullOrEmpty())
            {
                return "";
            }
            if (tools.Any(x => !(x is ToolCE)))
            {
                Log.Error($"Trying to get stat MeleeDamage from {optionalReq.Def.defName} which has no support for Combat Extended.");
                return "";
            }

            float lowestDamage = 999999f;
            float highestDamage = 0f;
            foreach (ToolCE tool in tools)
            {
                if (tool.power > highestDamage)
                {
                    highestDamage = tool.power;
                }
                if (tool.power < lowestDamage)
                {
                    lowestDamage = tool.power;
                }
            }

            return (lowestDamage*damageDeviationMin).ToStringByStyle(ToStringStyle.FloatMaxTwo) 
                + " - "
                + (highestDamage*damageDeviationMax).ToStringByStyle(ToStringStyle.FloatMaxTwo);
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            var tools = (req.Def as ThingDef)?.tools;

            if (tools.NullOrEmpty())
            {
                return base.GetExplanationUnfinalized(req, numberSense);
            }

            var stringBuilder = new StringBuilder();
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
                stringBuilder.AppendLine("    Base damage: " + tool.power.ToStringByStyle(ToStringStyle.FloatMaxTwo));
                stringBuilder.AppendLine(string.Format("    Damage variation: {0}% - {1}%", 100 * damageDeviationMin, 100 * damageDeviationMax));
                stringBuilder.AppendLine(string.Format("    Final value: {0} - {1}", tool.power * damageDeviationMin, tool.power * damageDeviationMax));
                stringBuilder.AppendLine();
            }
            return stringBuilder.ToString();
        }
    }
}
