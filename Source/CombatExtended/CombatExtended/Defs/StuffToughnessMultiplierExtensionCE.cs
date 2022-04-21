using Verse;

namespace CombatExtended
{
    /// <summary>
    /// Used in WeaponToughnessAutoPatcher for non-stuffable weapons and in StatWorker_WeaponToughness for stuffable weapons to affect the final toughness value.
    /// Assigned to ThingDefs as a mod extension. Acts as a value modifier.
    /// </summary>
    class StuffToughnessMultiplierExtensionCE : DefModExtension
    {
        public float toughnessMultiplier = 1f; // Value to multiply the original toughness value
    }
}
