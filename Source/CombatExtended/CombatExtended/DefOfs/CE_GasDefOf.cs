using RimWorld;

namespace CombatExtended;

[DefOf]
public static class CE_GasDefOf
{
    static CE_GasDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(CE_GasDefOf));
    }

    public static GasDef CE_BlackSmoke;
}
