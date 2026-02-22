using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CombatExtended;

/// <summary>
/// Defines a complete explosion spec applied to projectiles on impact, overriding the base projectile's explosion.
/// </summary>
public class TraitExplosionDef
{
    public float radius;
    public DamageDef damageDef;
    public int damageAmount = -1;
    public bool damageHitTarget = false;
}

public class CustomWeaponTraitDef : WeaponTraitDef
{
    public int magazineCapacityIncrease;
    public float magazineCapacityFactor = 1f;
    public CompProperties_UnderBarrel underBarrelProps;

    // CE verb overrides, replaces VEF's verbsOverride which breaks CE verbs
    public List<VerbPropertiesCE> verbsOverrideCE;
    public Dictionary<ThingDef, List<VerbPropertiesCE>> verbsOverridesCE;

    // Per-instance explosion override applied to projectiles on impact
    public TraitExplosionDef explosionOverride;

    // Random burst count multiplier selected per burst
    public List<float> burstShotCountMultipliers;

    // Override melee damage type
    public DamageDef meleeDamageOverride;

    // Allow indirect fire (lob over walls, high-arc trajectory like mortars)
    public bool allowIndirectFire;

    // Homing bullet acceleration (cells/s/s). If > 0, projectiles use HomingBulletTrajectoryWorker
    public float homingAcceleration;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }

        if (explosionOverride != null)
        {
            if (explosionOverride.radius <= 0f)
            {
                yield return "explosionOverride.radius must be > 0";
                if (explosionOverride.damageDef != null || explosionOverride.damageAmount >= 0)
                {
                    yield return "explosionOverride has damageDef/damageAmount but no radius — these will be ignored. Use damageDefOverride to change damage type without adding an explosion.";
                }
            }
        }
    }
}
