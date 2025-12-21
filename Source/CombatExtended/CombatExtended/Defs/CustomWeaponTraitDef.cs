using RimWorld;

namespace CombatExtended;

public class CustomWeaponTraitDef : WeaponTraitDef
{
    public int magazineCapacityIncrease;
    public float magazineCapacityFactor = 1f;
    public float shieldDamageMultiplier;
    public CompProperties_UnderBarrel underBarrelProps;

}
