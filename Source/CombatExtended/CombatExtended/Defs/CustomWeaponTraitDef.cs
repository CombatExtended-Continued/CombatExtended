using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CombatExtended;

public class CustomWeaponTraitDef : WeaponTraitDef
{
    public int magazineCapacityIncrease;
    public float magazineCapacityFactor = 1f;
    public CompProperties_UnderBarrel underBarrelProps;

    // CE verb overrides, replaces VEF's verbsOverride which breaks CE verbs
    public List<VerbPropertiesCE> verbsOverrideCE;
    public Dictionary<ThingDef, List<VerbPropertiesCE>> verbsOverridesCE;

    // Per-instance explosion added to projectiles on impact
    public float explosionRadius;
    public DamageDef explosionDamageDef;
    public int explosionDamageAmount = -1;

    // Random burst count multiplier selected per burst
    public List<float> burstShotCountMultipliers;

    // Override melee damage type
    public DamageDef meleeDamageOverride;
}
