using RimWorld;
using Verse;

namespace CombatExtended
{
    public class StatWorker_BodyPartBluntArmor : StatWorker_BodyPartDensity
    {
        protected override string UnitString => "CE_MPa".Translate();
        protected override float GetBaseValueFor(StatRequest req)
        {
            var pawn = (Pawn)req.Thing;
            return pawn.RaceProps.IsFlesh ? 0.72f : 2f;
        }
    }
}