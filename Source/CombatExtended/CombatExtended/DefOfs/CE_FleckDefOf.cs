using RimWorld;
using Verse;

namespace CombatExtended
{
    [DefOf]
    public class CE_FleckDefOf
    {
        static CE_FleckDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CE_FleckDefOf));
        }
        //public static FleckDef Fleck_SuppressIcon;

        //public static FleckDef Fleck_HunkerIcon;

        //vanilla defs that didn't have a DefOf
        public static FleckDef BlastFlame;
        public static FleckDef ElectricalSpark;
        //CE defs
        public static FleckDef Fleck_HeatGlow_API;
        public static FleckDef Fleck_BulletHole;
        public static FleckDef Fleck_ElectricGlow_EMP;
        public static FleckDef Fleck_SparkThrownFast;
        public static FleckDef Fleck_EmptyCasing;
    }
}
