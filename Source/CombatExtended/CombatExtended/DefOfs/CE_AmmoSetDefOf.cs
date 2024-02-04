using RimWorld;

namespace CombatExtended
{
    [DefOf]
    public static class CE_AmmoSetDefOf
    {
        static CE_AmmoSetDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CE_AmmoSetDefOf));
        }
        public static AmmoSetDef AmmoSet_81mmMortarShell;
    }
}
