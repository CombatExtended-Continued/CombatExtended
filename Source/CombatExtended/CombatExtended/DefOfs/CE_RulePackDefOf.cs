using RimWorld;
using Verse;

namespace CombatExtended
{
    [DefOf]
    public static class CE_RulePackDefOf
    {
        static CE_RulePackDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CE_RulePackDefOf));
        }
        public static RulePackDef AttackMote;
        public static RulePackDef SuppressedMote;
        public static RulePackDef DamageEvent_ShellingExplosion;
        public static RulePackDef DamageEvent_CookOff;
        public static RulePackDef DamageEvent_Shelling;
    }
}
