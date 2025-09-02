using RimWorld;
using Verse;

namespace CombatExtended;
[DefOf]
public static class CE_SoundDefOf
{
    static CE_SoundDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(CE_SoundDefOf));
    }
    public static SoundDef CE_AutoLoaderAmbient;
    public static SoundDef Interact_Bipod;
}
