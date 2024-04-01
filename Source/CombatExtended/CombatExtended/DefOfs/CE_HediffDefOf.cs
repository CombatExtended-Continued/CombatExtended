using RimWorld;
using Verse;

namespace CombatExtended
{
    [DefOf]
    public static class CE_HediffDefOf
    {
        static CE_HediffDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CE_HediffDefOf));
        }
        public static HediffDef VenomBuildup;
        public static HediffDef SmokeInhalation;
        public static HediffDef MuscleSpasms;
    }
}
