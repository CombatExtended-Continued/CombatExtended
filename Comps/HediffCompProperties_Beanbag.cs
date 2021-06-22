using Verse;

namespace CombatExtended
{
    public class HediffCompProperties_Beanbag : HediffCompProperties
    {
        public float BaseSeverityPerDamage = 0.05f;

        public HediffCompProperties_Beanbag()
        {
            compClass = typeof(HediffComp_Beanbag);
        }
    }
}