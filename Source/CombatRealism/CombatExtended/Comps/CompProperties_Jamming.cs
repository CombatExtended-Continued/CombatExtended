using Verse;

namespace CombatExtended
{
    public class CompProperties_Jamming : CompProperties
    {
        public float baseMalfunctionChance = 0f;
        public bool canExplode = false;
        public float explosionDamage = 0f;
        public float explosionRadius = 1f;
        public SoundDef explosionSound = null;

        public CompProperties_Jamming()
        {
            compClass = typeof(CompJamming);
        }
    }
}
