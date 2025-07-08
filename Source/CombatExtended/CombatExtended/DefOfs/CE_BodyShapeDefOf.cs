using RimWorld;

namespace CombatExtended;
[DefOf]
public class CE_BodyShapeDefOf
{
    static CE_BodyShapeDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(CE_BodyShapeDefOf));
    }
    public static BodyShapeDef Invalid;
    public static BodyShapeDef Humanoid;
    public static BodyShapeDef Quadruped;
}
