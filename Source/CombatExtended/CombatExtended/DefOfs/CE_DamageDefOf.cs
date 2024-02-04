using RimWorld;
using Verse;

namespace CombatExtended
{
    [DefOf]
    public class CE_DamageDefOf
    {
        static CE_DamageDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CE_DamageDefOf));
        }
        public static DamageDef Electrical;
        public static DamageDef Flame_Secondary;
        public static DamageDef Bomb_Secondary;
        public static DamageDef PrometheumFlame;
        public static DamageDef Bomb;
        public static DamageDef EMP;
    }
}
