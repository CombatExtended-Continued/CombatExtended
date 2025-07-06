using RimWorld;
using Verse;

namespace CombatExtended;
[DefOf]
public class CE_DamageArmorCategoryDefOf
{
    static CE_DamageArmorCategoryDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(CE_DamageArmorCategoryDefOf));
    }
    public static DamageArmorCategoryDef Blunt;
    public static DamageArmorCategoryDef Heat;
    public static DamageArmorCategoryDef Electric;
}
