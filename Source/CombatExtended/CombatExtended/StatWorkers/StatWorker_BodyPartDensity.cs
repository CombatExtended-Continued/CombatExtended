using RimWorld;
using Verse;

namespace CombatExtended
{
    public class StatWorker_BodyPartDensity : StatWorker
    {
        private const float HealthScaleFactor = 0.05f;  // What % of health scale to use for density

        public override bool ShouldShowFor(StatRequest req)
        {
            return base.ShouldShowFor(req) && req.Thing is Pawn;
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            var pawn = (Pawn)req.Thing;
            return pawn.HealthScale * HealthScaleFactor;
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            var pawn = (Pawn) req.Thing;
            return $"{"CE_StatsReport_PartDensity".Translate().Trim()}: {pawn.HealthScale} x {HealthScaleFactor}";
        }
    }
}