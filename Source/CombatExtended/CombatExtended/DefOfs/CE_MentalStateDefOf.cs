using RimWorld;
using Verse;

namespace CombatExtended
{
    [DefOf]
    public class CE_MentalStateDefOf
    {
        static CE_MentalStateDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CE_MentalStateDefOf));
        }
        public static MentalStateDef ShellShock;
        public static MentalStateDef CombatFrenzy;
        public static MentalStateDef WanderConfused;
    }
}
