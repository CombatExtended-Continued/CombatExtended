using RimWorld;
using Verse;

namespace CombatExtended
{
    public class StatWorker_BodyPartRHA : StatWorker_BodyPartDensity
    {
        protected override string UnitString => "CE_mmRHA".Translate();
        protected override float GetBaseValueFor(StatRequest req)
        {
            var pawn = (Pawn)req.Thing;
            return 20 * (pawn.RaceProps.IsFlesh ? 0.011f : 0.8f);
        }
    }
}