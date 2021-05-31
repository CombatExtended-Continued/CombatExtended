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
    public class StatWorker_MeleeDamage : StatWorker_MeleeDamageBase
    {

        public override string GetStatDrawEntryLabel(StatDef stat, float value, ToStringNumberSense numberSense, StatRequest optionalReq, bool finalized = true)
        {
            return GetFinalDisplayValue(optionalReq);
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            var skilledDamageVariationMin = damageVariationMin;
            var skilledDamageVariationMax = damageVariationMax;
            var meleeSkillLevel = -1;

            if (req.Thing?.ParentHolder is Pawn_EquipmentTracker tracker && tracker.pawn != null)
            {
                var pawnHolder = tracker.pawn;
                skilledDamageVariationMin = GetDamageVariationMin(pawnHolder);
                skilledDamageVariationMax = GetDamageVariationMax(pawnHolder);

                if (pawnHolder.skills != null)
                    meleeSkillLevel = pawnHolder.skills.GetSkill(SkillDefOf.Melee).Level;
            }

            var tools = (req.Def as ThingDef)?.tools;

            if (tools.NullOrEmpty())
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

            foreach (ToolCE tool in tools)
            {
                var adjustedToolDamage = GetAdjustedDamage(tool, req.Thing);
                var maneuvers = DefDatabase<ManeuverDef>.AllDefsListForReading.Where(d => tool.capacities.Contains(d.requiredCapacity));
                var maneuverString = "(";
                foreach (var maneuver in maneuvers)
                {
                    maneuverString += maneuver.ToString() + "/";
                }
                maneuverString = maneuverString.TrimmedToLength(maneuverString.Length - 1) + ")";
                stringBuilder.AppendLine("  " + "Tool".Translate() + ": " + tool.ToString() + " " + maneuverString);
                stringBuilder.AppendLine("    " + "CE_DescBaseDamage".Translate() + ": " + tool.power.ToStringByStyle(ToStringStyle.FloatMaxTwo));
                stringBuilder.AppendLine("    " + "CE_AdjustedForWeapon".Translate() + ": " + adjustedToolDamage.ToStringByStyle(ToStringStyle.FloatMaxTwo));
                stringBuilder.AppendLine(string.Format("    " + "StatsReport_FinalValue".Translate() + ": {0} - {1}",
                    (adjustedToolDamage * skilledDamageVariationMin).ToStringByStyle(ToStringStyle.FloatMaxTwo),
                    (adjustedToolDamage * skilledDamageVariationMax).ToStringByStyle(ToStringStyle.FloatMaxTwo)));
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
            var skilledDamageVariationMin = damageVariationMin;
            var skilledDamageVariationMax = damageVariationMax;

            if (optionalReq.Thing?.ParentHolder is Pawn_EquipmentTracker tracker)
            {
                skilledDamageVariationMin = GetDamageVariationMin(tracker.pawn);
                skilledDamageVariationMax = GetDamageVariationMax(tracker.pawn);
            }

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

            float lowestDamage = Int32.MaxValue;
            float highestDamage = 0f;
            foreach (ToolCE tool in tools)
            {
                var toolDamage = GetAdjustedDamage(tool, optionalReq.Thing);
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
}
