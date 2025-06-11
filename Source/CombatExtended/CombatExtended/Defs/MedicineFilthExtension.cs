using Verse;

namespace CombatExtended
{
    public class MedicineFilthExtension : DefModExtension
    {
        public ThingDef filthDefName;
        public float filthSpawnChance = 1;
        public IntRange filthSpawnQuantity = IntRange.One;
    }
}
