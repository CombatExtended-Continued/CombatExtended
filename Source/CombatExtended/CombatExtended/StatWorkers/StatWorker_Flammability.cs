using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    [StaticConstructorOnStartup]
    public class StatWorker_Flammability : StatWorker
    {
        private static readonly SimpleCurve HumidityCurve = new SimpleCurve();

        static StatWorker_Flammability()
        {
            HumidityCurve.Add(0, FireSpread.values.flammabilityHumidityMin);
            HumidityCurve.Add(0.5f, FireSpread.values.flammabilityHumidityHalf);
            HumidityCurve.Add(1, FireSpread.values.flammabilityHumidityMax);
        }

        private static float GetPrecipitationFactorFor(Thing plant)
        {
            var tracker = plant.MapHeld.GetComponent<WeatherTracker>();
            return HumidityCurve.Evaluate(tracker.HumidityPercent);
        }

        private static void GetApparelAdjustFor(Pawn pawn, out float apparelFlammability, out float apparelCoverage)
        {
            apparelFlammability = 0f;
            apparelCoverage = 0f;
            foreach (var part in pawn.RaceProps.body.AllParts)
            {
                var apparels = pawn.apparel?.WornApparel.FindAll(a => a.def.apparel.CoversBodyPart(part));
                if (apparels == null || !apparels.Any())
                    continue;

                apparels.SortBy(a => a.GetStatValue(StatDefOf.Flammability));
                var apparel = apparels.First();
                apparelFlammability += apparel.GetStatValue(StatDefOf.Flammability) * part.coverageAbs;
                apparelCoverage += part.coverageAbs;
            }
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            var flammability = base.GetValueUnfinalized(req, applyPostProcess);
            if (req.Thing is Plant plant)
                return flammability * GetPrecipitationFactorFor(plant);

            if (req.Thing is Pawn pawn && pawn.apparel?.WornApparelCount > 0)
            {
                GetApparelAdjustFor(pawn, out var totalFlammability, out var totalCoverage);
                totalFlammability += flammability * (1 - totalCoverage);
                return flammability - (1 - totalFlammability);
            }

            return flammability;
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            var explanation = base.GetExplanationUnfinalized(req, numberSense);
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(explanation);

            if (req.Thing is Plant plant)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine();
                stringBuilder.AppendLine(
                    $"{"CE_StatsReport_FlammabilityPrecipitation".Translate().Trim()}: {GetPrecipitationFactorFor(plant).ToStringByStyle(ToStringStyle.PercentZero)}");
            }

            if (req.Thing is Pawn pawn && pawn.apparel?.WornApparelCount > 0)
            {
                GetApparelAdjustFor(pawn, out var apparelFlammability, out var apparelCoverage);
                apparelFlammability /= apparelCoverage;

                stringBuilder.AppendLine();
                stringBuilder.AppendLine();
                stringBuilder.AppendLine(
                    $"{"CE_StatsReport_FlammabilityApparelFactor".Translate().Trim()}: {apparelFlammability.ToStringByStyle(ToStringStyle.PercentZero)}");
                stringBuilder.AppendLine(
                    $"{"CE_StatsReport_FlammabilityApparelCoverage".Translate().Trim()}: {apparelCoverage.ToStringByStyle(ToStringStyle.PercentZero)}");
            }

            return stringBuilder.ToString();
        }
    }
}