using RimWorld;
using Verse;

namespace CombatExtended
{
    [DefOf]
    public class CE_BodyPartGroupDefOf
    {
        static CE_BodyPartGroupDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CE_BodyPartGroupDefOf));
        }
        public static BodyPartGroupDef CoveredByNaturalArmor;
        public static BodyPartGroupDef RightArm;
        public static BodyPartGroupDef LeftArm;
        public static BodyPartGroupDef LeftShoulder;
        public static BodyPartGroupDef HeadAttackTool;
    }
}
