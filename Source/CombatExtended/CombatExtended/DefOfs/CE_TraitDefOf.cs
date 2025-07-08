using RimWorld;

namespace CombatExtended;
[DefOf]
public class CE_TraitDefOf
{
    static CE_TraitDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(CE_TraitDefOf));
    }
    public static TraitDef Bravery;
}
