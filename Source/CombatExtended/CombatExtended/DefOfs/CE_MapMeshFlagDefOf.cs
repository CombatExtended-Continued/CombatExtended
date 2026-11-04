using RimWorld;

namespace CombatExtended;

[DefOf]
public static class CE_MapMeshFlagDefOf
{
    static CE_MapMeshFlagDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(CE_MapMeshFlagDefOf));
    }

    public static MapMeshFlagDef CE_Gas;
}
