using System.Text;
using RimWorld;
using Verse;

namespace CombatExtended
{
    public abstract class StatWorker_BodyPartDensity : StatWorker
    {
        public abstract string UnitString { get; }

        public abstract new float GetBaseValueFor(StatRequest req);

        public override bool ShouldShowFor(StatRequest req)
        {
            return base.ShouldShowFor(req) && req.Thing is Pawn;
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            var baseValue = GetBaseValueFor(req);
            var pawn = (Pawn)req.Thing;
            return baseValue * pawn.HealthScale;
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"{"CE_StatsReport_BaseValue".Translate()}: {GetBaseValueFor(req)} {UnitString}");
            stringBuilder.AppendLine();

            var pawn = (Pawn) req.Thing;
            stringBuilder.AppendLine($"{"StatsReport_HealthMultiplier".Translate(pawn.HealthScale)}: x{pawn.HealthScale.ToStringPercent()}");
            stringBuilder.AppendLine();

            return stringBuilder.ToString().Trim();
        }
    }
}
