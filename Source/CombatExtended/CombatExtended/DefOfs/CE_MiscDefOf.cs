using RimWorld;

namespace CombatExtended;
[DefOf]
public class CE_MiscDefOf
{
    public static AmmoInjectorOptions ammoInjectorOptions;

    static CE_MiscDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(CE_MiscDefOf));
    }
}
