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

        private float GetMeleeDamage(StatRequest req)
        {
            var tools = (req.Def as ThingDef)?.tools;
            if (tools.NullOrEmpty())
            {
                return 0;
            }
            if (tools.Any(x=> !(x is ToolCE)))
            {
                Log.Error($"Trying to get stat MeleeDamage from {req.Def.defName} which has no support for Combat Extended.");
                return 0;
            }

            float totalSelectionWeight = 0f;
            for (int i = 0; i < tools.Count; i++)
            {
                totalSelectionWeight += tools[i].chanceFactor;
            }
            float totalAverageDamage = 0f;
            foreach (ToolCE tool in tools)
            {
                var weightFactor = tool.chanceFactor / totalSelectionWeight;
                totalAverageDamage += weightFactor * tool.power;
            }
            return totalAverageDamage;
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            var baseDamage = GetMeleeDamage(req);
            return UnityEngine.Random.Range(baseDamage * damageDeviationMin, baseDamage * damageDeviationMax);
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
                stringBuilder.AppendLine(string.Format("    Damage variation: {0}% ~ {1}%", 100 * damageDeviationMin, 100 * damageDeviationMax));
                stringBuilder.AppendLine(string.Format("    Final value: {0} ~ {1}", tool.power * damageDeviationMin, tool.power * damageDeviationMax));
                stringBuilder.AppendLine();
            }
            return stringBuilder.ToString();
        }
    }
}
