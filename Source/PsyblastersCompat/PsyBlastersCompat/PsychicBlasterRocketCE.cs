using HarmonyLib;
using PsyBlasters;
using RimWorld;
using Verse;

namespace CombatExtended.Compatibility.PsyBlastersCompat
{
    public class PsychicBlasterRocketCE : ProjectileCE_Explosive
    {
        PsyBlasterBulletComp _psyBlasterBulletComp => GetComp<PsyBlasterBulletComp>();

        private bool CanConsumeResources(Pawn launcherPawn)
        {
            return _psyBlasterBulletComp != null && launcherPawn is { HasPsylink: true };
        }

        public override float DamageAmount
        {
            get
            {
                if (CanConsumeResources(launcher as Pawn))
                {
                    return def.projectile.GetDamageAmount(
                               equipment?.GetStatValue(StatDefOf.RangedWeapon_DamageMultiplier) ?? 1f) +
                           ((((Pawn)launcher).psychicEntropy.MaxPotentialEntropy -
                             ((Pawn)launcher).psychicEntropy.EntropyValue) * _psyBlasterBulletComp.PsyDamageMulti);
                }
                return 0;
            }
        }

        public override void Impact(Thing hitThing) //that's also copied from the original
        {
            base.Impact(hitThing);

            if (!CanConsumeResources(launcher as Pawn))
            {
                return;
            }

            var launcherPawn = (Pawn)launcher;
            Traverse.Create(launcherPawn).Field("psychicEntropy").Field("currentEntropy")
                .SetValue(launcherPawn.psychicEntropy.MaxPotentialEntropy * 5.5f);
        }
    }

}

