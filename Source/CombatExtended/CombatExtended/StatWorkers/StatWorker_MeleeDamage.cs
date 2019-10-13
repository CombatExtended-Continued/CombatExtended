using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class StatWorker_MeleeDamage : StatWorker_MeleeDamageBase
    {

        public override string GetStatDrawEntryLabel(StatDef stat, float value, ToStringNumberSense numberSense, StatRequest optionalReq)
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
                if (tool.power > highestDamage)
                {
                    highestDamage = tool.power;
                }
                if (tool.power < lowestDamage)
                {
                    lowestDamage = tool.power;
                }
            }

            return (lowestDamage * skilledDamageVariationMin).ToStringByStyle(ToStringStyle.FloatMaxTwo)
                + " - "
                + (highestDamage * skilledDamageVariationMax).ToStringByStyle(ToStringStyle.FloatMaxTwo);
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
                stringBuilder.AppendLine("Wielder skill level: " + meleeSkillLevel);
            }
            stringBuilder.AppendLine(string.Format("Damage variation: {0}% - {1}%",
                (100 * skilledDamageVariationMin).ToStringByStyle(ToStringStyle.FloatMaxTwo),
                (100 * skilledDamageVariationMax).ToStringByStyle(ToStringStyle.FloatMaxTwo)));
            stringBuilder.AppendLine("");

            foreach (ToolCE tool in tools)
            {
                var maneuvers = DefDatabase<ManeuverDef>.AllDefsListForReading.Where(d => tool.capacities.Contains(d.requiredCapacity));
                var maneuverString = "(";
                foreach (var maneuver in maneuvers)
                {
                    maneuverString += maneuver.ToString() + "/";
                }
                maneuverString = maneuverString.TrimmedToLength(maneuverString.Length - 1) + ")";
                stringBuilder.AppendLine("  Tool: " + tool.ToString() + " " + maneuverString);
                stringBuilder.AppendLine("    Base damage: " + tool.power.ToStringByStyle(ToStringStyle.FloatMaxTwo));
                stringBuilder.AppendLine(string.Format("    Final value: {0} - {1}",
                    (tool.power * skilledDamageVariationMin).ToStringByStyle(ToStringStyle.FloatMaxTwo),
                    (tool.power * skilledDamageVariationMax).ToStringByStyle(ToStringStyle.FloatMaxTwo)));
                stringBuilder.AppendLine();
            }
            return stringBuilder.ToString();
        }

    }
}
