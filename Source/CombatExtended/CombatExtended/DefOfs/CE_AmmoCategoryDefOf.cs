using RimWorld;

namespace CombatExtended
{
    [DefOf]
    public class CE_AmmoCategoryDefOf
    {

        static CE_AmmoCategoryDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CE_AmmoCategoryDefOf));
        }
        public static AmmoCategoryDef ExplosiveAP;
    }
}
