using Verse;

namespace CombatExtended
{
    public class HediffCompProperties_Venom : HediffCompProperties
    {
        public float VenomPerSeverity;
        public int MinTicks;
        public int MaxTicks;

        public HediffCompProperties_Venom()
        {
            compClass = typeof(HediffComp_Venom);
        }
    }
}