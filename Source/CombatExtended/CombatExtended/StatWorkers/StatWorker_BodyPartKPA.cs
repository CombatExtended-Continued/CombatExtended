using RimWorld;
using Verse;

namespace CombatExtended
{
    public class StatWorker_BodyPartKPA : StatWorker_BodyPartDensity
    {
        protected override string UnitString => "CE_kPa".Translate();
        protected override float GetBaseValueFor(StatRequest req)
        {
            var pawn = (Pawn)req.Thing;
            return pawn.RaceProps.IsFlesh ? 720 : 8000;
        }
    }
}