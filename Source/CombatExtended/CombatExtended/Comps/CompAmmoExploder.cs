using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    [StaticConstructorOnStartup]
    public class PatchesBPs
    {
       static PatchesBPs()
        {
            CE_BodyPartDefOf.MechanicalCapacitor.modExtensions = new List<DefModExtension>();

            CE_BodyPartDefOf.MechanicalCapacitor.modExtensions.Add(new BodyPartExploderExt { triggerChance = 1f, allowedDamageDefs = new List<DamageDef> { DamageDefOf.Burn, DamageDefOf.Bullet, CE_DamageDefOf.Flame_Secondary } });
        }
    }

    public class CompAmmoExploder : ThingComp
    {
        public DamageDef damageDef;

        public override void PostPreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {
            damageDef = dinfo.Def;
            base.PostPreApplyDamage(dinfo, out absorbed);
        }
        public void PostDamageResult(DamageWorker.DamageResult damage)
        {
            var damExt = damage?.parts?.Find(x => x.def.HasModExtension<BodyPartExploderExt>())?.def?.GetModExtension<BodyPartExploderExt>() ?? null;
            if (damExt != null)
            {
                if (
                    damageDef != null
                    &&
                    Rand.Chance(damExt.triggerChance)
                    &&
                    damExt.allowedDamageDefs.Contains(damageDef)
                    )
                {
                    DetonateCarriedAmmo();
                }
            }
        }
        public void DetonateCarriedAmmo()
        {
            if (!(((Pawn)this.parent)?.inventory?.innerContainer?.Where(x => x is AmmoThing).EnumerableNullOrEmpty() ?? true))
            {
                foreach (AmmoThing ammo in ((Pawn)this.parent).inventory.innerContainer.Where(x => x is AmmoThing))
                {
                    var projdef = ((AmmoDef)ammo.def)?.AmmoSetDefs[0]?.ammoTypes[0]?.projectile ?? null;
                    var proj = (projdef?.projectile ?? null) as ProjectilePropertiesCE;
                    if (proj != null)
                    {
                        if (proj.explosionRadius > 0f)
                        {
                            GenExplosionCE.DoExplosion
                                (
                                    this.parent.Position,
                                    this.parent.Map,
                                    proj.explosionRadius,
                                    proj.damageDef,
                                    null,
                                    proj.GetDamageAmount(1f),
                                    proj.armorPenetrationSharp,
                                    postExplosionSpawnChance: proj.postExplosionSpawnChance,
                                    postExplosionSpawnThingCount: proj.postExplosionSpawnThingCount,
                                    postExplosionSpawnThingDef: proj.postExplosionSpawnThingDef,
                                    preExplosionSpawnChance: proj.preExplosionSpawnChance,
                                    preExplosionSpawnThingCount: proj.preExplosionSpawnThingCount,
                                    preExplosionSpawnThingDef: proj.preExplosionSpawnThingDef

                                );
                        }
                    }

                }
            }
            
        }
    }
}
