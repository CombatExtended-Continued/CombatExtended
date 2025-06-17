using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended.Compatibility.PsyBlastersCompat
{
    public class PsychicBlasterRocketCE : ProjectileCE_Explosive
    {
        PsychicProjectileExtension psyModExtension => def.GetModExtension<PsychicProjectileExtension>();
        private float _damageAmount;

        private bool CanConsumeResources(Pawn launcherPawn)
        {
            return psyModExtension != null && launcherPawn is { HasPsylink: true };
        }

        public override float DamageAmount => _damageAmount;
        
        

        public override void Launch(Thing launcher, Vector2 origin, Thing equipment = null)
        {
            base.Launch(launcher, origin, equipment);
            if (!CanConsumeResources(launcher as Pawn))
            {
                _damageAmount = 0;
                return;
            }
            
            _damageAmount = def.projectile.GetDamageAmount(equipment?.GetStatValue(StatDefOf.RangedWeapon_DamageMultiplier) ?? 1f) +
                            ((((Pawn)launcher).psychicEntropy.MaxPotentialEntropy -
                              ((Pawn)launcher).psychicEntropy.EntropyValue) * psyModExtension.psyDamageMultiplier);
            
            Pawn launcherPawn = (Pawn)launcher;
            Traverse.Create(launcherPawn).Field("psychicEntropy").Field("currentEntropy")
                .SetValue(launcherPawn.psychicEntropy.MaxPotentialEntropy * 5.5f);
        }
    }

}

