using PsyBlasters;
using Verse;
using RimWorld;

namespace CombatExtended.Compatibility.PsyBlastersCompat
{
    public class PsychicBlasterBulletCE : BulletCE //Basically just duplicating the original behavior to the CE class
    {
        private PsyBlasterBulletComp _psyBlasterBulletComp => GetComp<PsyBlasterBulletComp>();

        private bool CanConsumeResources(Pawn launcherPawn)
        {
            return _psyBlasterBulletComp != null &&
                   launcherPawn is { HasPsylink: true, psychicEntropy.CurrentPsyfocus: > 0 };
        }

        public override float DamageAmount
        {
            get
            {
                var damMulti = equipment?.GetStatValue(StatDefOf.RangedWeapon_DamageMultiplier) ?? 1f;
                if (CanConsumeResources(launcher as Pawn))
                {
                    damMulti += _psyBlasterBulletComp.PsyDamageMulti;
                }

                return def.projectile.GetDamageAmount(damMulti);
            }
        }

        public override float PenetrationAmount
        {
            get
            {
                var projectilePropsCE = (ProjectilePropertiesCE)def.projectile;
                var isSharp = def.projectile.damageDef.armorCategory == DamageArmorCategoryDefOf.Sharp;
                var penMulti = equipment?.GetStatValue(StatDefOf.RangedWeapon_DamageMultiplier) ?? 1f;
                if (CanConsumeResources(launcher as Pawn))
                {
                    penMulti += _psyBlasterBulletComp.PsyPenMulti;
                }

                return penMulti *
                       (isSharp ? projectilePropsCE.armorPenetrationSharp : projectilePropsCE.armorPenetrationBlunt);
            }
        }

        public override void Impact(Thing hitThing)
        {
            base.Impact(hitThing);

            if (hitThing is not Pawn && Rand.Chance(0.66f) //don't look at me, it was like that in the original code
                || launcher is not Pawn launcherPawn
                || !CanConsumeResources(launcherPawn)
                || launcherPawn.psychicEntropy.limitEntropyAmount &&
                launcherPawn.psychicEntropy.WouldOverflowEntropy(_psyBlasterBulletComp.EntropyCost))
            {
                return;
            }

            launcherPawn.psychicEntropy.OffsetPsyfocusDirectly(-_psyBlasterBulletComp.PsyCost);
            launcherPawn.psychicEntropy.TryAddEntropy(_psyBlasterBulletComp.EntropyCost);
        }
    }
}


