using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace CombatExtended
{
    [StaticConstructorOnStartup]
    public class GunPatcher
    {
        static GunPatcher()
        {
            var unpatchedGuns = DefDatabase<ThingDef>.AllDefs.Where(x => x.IsRangedWeapon && (x.verbs?.Any(x => !(x is VerbPropertiesCE)) ?? false));

            var patcherDefs = DefDatabase<GunPatcherPresetDef>.AllDefs;

            foreach (var preset in patcherDefs)
            {
                unpatchedGuns.PatchGunsFromPreset(preset);
            }

            if (unpatchedGuns.Count() > 0)
            {
                foreach (var gun in unpatchedGuns)
                {
                    gun.PatchGunFromPreset(
                        patcherDefs.MaxBy
                        (
                            //random range is there to avoid elements with same values
                            x =>
                            (x.DamageRange.Average - (gun.Verbs[0]?.defaultProjectile?.projectile.GetDamageAmount(1f) ?? Rand.Range(-800f, -900f)))
                            +
                            (x.RangeRange.Average - (gun.Verbs[0]?.range ?? Rand.Range(-800f, -900f)))
                            +
                            (x.ProjSpeedRange.Average - (gun.Verbs[0]?.defaultProjectile?.projectile.speed ?? Rand.Range(-800f, -900f)))
                            +
                            (x.WarmupRange.Average - (gun.Verbs[0]?.warmupTime ?? Rand.Range(-800f, -900f)))

                            ));
                }
            }
        }
    }
}
