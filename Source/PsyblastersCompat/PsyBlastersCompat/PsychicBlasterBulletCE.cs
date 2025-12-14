using Verse;
using RimWorld;
using UnityEngine;

namespace CombatExtended.Compatibility.PsyBlastersCompat;
public class PsychicBlasterBulletCE : BulletCE //Basically just duplicating the original behavior to the CE class
{
    private PsychicProjectileExtension psyModExtension => def.GetModExtension<PsychicProjectileExtension>();
    private float _damageAmount;
    private float _penetrationAmount;

    private bool CanConsumeResources(Pawn launcherPawn, float amount)
    {
        return psyModExtension != null &&
               launcherPawn is { HasPsylink: true, psychicEntropy.CurrentPsyfocus: > 0 } validPawn && !validPawn.psychicEntropy.WouldOverflowEntropy(amount);
    }

    public override float DamageAmount => _damageAmount;

    public override float PenetrationAmount => _penetrationAmount;

    public override void Launch(Thing launcher, Vector2 origin, Thing equipment = null)
    {
        base.Launch(launcher, origin, equipment);
        var projectilePropsCE = (ProjectilePropertiesCE)def.projectile;
        var damageMultiplier = equipment?.GetStatValue(StatDefOf.RangedWeapon_DamageMultiplier) ?? 1f;
        var penMultiplier = damageMultiplier;
        bool isSharp = projectilePropsCE.damageDef.armorCategory == DamageArmorCategoryDefOf.Sharp;
        if (CanConsumeResources(launcher as Pawn, psyModExtension.entropyCost))
        {
            Pawn launcherPawn = launcher as Pawn;
            penMultiplier += psyModExtension.psyPenMultiplier * (psyModExtension.scaleFromSensitivity ? launcherPawn.psychicEntropy.PsychicSensitivity : 1f);
            damageMultiplier += psyModExtension.psyDamageMultiplier * (psyModExtension.scaleFromSensitivity ? launcherPawn.psychicEntropy.PsychicSensitivity : 1f);
            launcherPawn.psychicEntropy.OffsetPsyfocusDirectly(psyModExtension.psyfocusCost);
            launcherPawn.psychicEntropy.TryAddEntropy(psyModExtension.entropyCost);
        }
        _damageAmount = def.projectile.GetDamageAmount(damageMultiplier, null);
        _penetrationAmount = penMultiplier * (isSharp ? projectilePropsCE.armorPenetrationSharp : projectilePropsCE.armorPenetrationBlunt);
    }
}


