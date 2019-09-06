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
            HumidityCurve.Add(0, 4);
            HumidityCurve.Add(0.5f, 1);
            HumidityCurve.Add(1, 0.25f);
        }

        private static float GetPrecipitationFactorFor(Thing plant)
        {
            var tracker = plant.MapHeld.GetComponent<WeatherTracker>();
            return HumidityCurve.Evaluate(tracker.HumidityPercent);
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            var flammability = base.GetValueUnfinalized(req, applyPostProcess);
            if (!(req.Thing is Plant plant))
                return flammability;

            return flammability * GetPrecipitationFactorFor(plant);
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            var explanation = base.GetExplanationUnfinalized(req, numberSense);

            if (!(req.Thing is Plant plant))
                return explanation;

            var stringBuilder = new StringBuilder();
            stringBuilder.Append(explanation);
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(
                $"{"CE_StatsReport_FlammabilityPrecipitation".Translate().Trim()}: {GetPrecipitationFactorFor(plant).ToStringByStyle(ToStringStyle.PercentZero)}");

            return stringBuilder.ToString();
        }
    }
}