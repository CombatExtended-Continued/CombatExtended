using CombatExtended.WorldObjects;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class WorldObjectDamageWorker
    {
        #region WorldObject
        public virtual float ApplyDamage(HealthComp healthComp, ThingDef shellDef)
        {
            var damage = CalculateDamage(shellDef.GetProjectile(), healthComp.parent.Faction) / healthComp.ArmorDamageMultiplier;
            healthComp.Health -= damage;
            return damage;
        }
        public virtual float CalculateDamage(ThingDef projectile, Faction faction)
        {
            var empModifier = 0.03f;
            if (faction != null)
            {
                if ((int)faction.def.techLevel > (int)TechLevel.Medieval)
                {
                    empModifier = Mathf.Pow(3.3f, (int)faction.def.techLevel) / 1800f / ((int)faction.def.techLevel - 2f);
                }
                if (faction.def.pawnGroupMakers.SelectMany(x => x.options).All(k => k.kind.RaceProps.IsMechanoid))
                {
                    empModifier = 1f;
                }
            }
            var result = FragmentsPotentialDamage(projectile) + ExplosionPotentialDamage(projectile) + FirePotentialDamage(projectile) + EMPPotentialDamage(projectile, empModifier);
            //Damage calculated as in-map damage, needs to be converted into world object damage. 3500f experimentally obtained
            result /= 3500f;
            //Crit/Miss imitation
            result *= Rand.Range(0.4f, 1.5f);
            if (projectile.projectile is ProjectilePropertiesCE projectileProperties && projectileProperties.shellingProps.damage > 0f)
            {
                result *= projectileProperties.shellingProps.damage;
            }
            return result;
        }
        protected const float fragDamageMultipler = 0.04f;
        protected virtual float FragmentsPotentialDamage(ThingDef projectile)
        {
            var result = 0f;
            var fragProp = projectile.GetCompProperties<CompProperties_Fragments>();
            if (projectile.projectile is ProjectilePropertiesCE props && fragProp != null)
            {
                foreach (var frag in fragProp.fragments)
                {
                    result += frag.count * frag.thingDef.projectile.damageAmountBase * fragDamageMultipler;
                }
            }
            return result;
        }
        protected virtual float EMPPotentialDamage(ThingDef projectile, float modifier = 0.03f)
        {
            float result = 0f;
            if (projectile.projectile is ProjectilePropertiesCE props && props.damageDef == DamageDefOf.EMP)
            {
                result += props.damageAmountBase * modifier;
                for (int i = 1; i < props.explosionRadius; i++)
                {
                    result += modifier * DamageAtRadius(projectile, i) * Mathf.Pow(2, i);
                }
            }
            return result;
        }
        protected virtual float FirePotentialDamage(ThingDef projectile)
        {
            const float prometheumDamagePerCell = 3;
            float result = 0f;
            if (projectile.projectile is ProjectilePropertiesCE props && props.damageDef == CE_DamageDefOf.PrometheumFlame)
            {
                result += props.damageAmountBase;
                for (int i = 1; i < props.explosionRadius; i++)
                {
                    result += DamageAtRadius(projectile, i) * Mathf.Pow(2, i);
                }
                if (props.preExplosionSpawnThingDef == CE_ThingDefOf.FilthPrometheum)
                {
                    result += props.preExplosionSpawnChance * (Mathf.PI * props.explosionRadius * props.explosionRadius) * prometheumDamagePerCell; // damage per cell with preExplosionSpawn Chance
                }
            }
            return result;
        }
        protected virtual float ExplosionPotentialDamage(ThingDef projectile)
        {
            float result = 0f;
            if (projectile.projectile is ProjectilePropertiesCE props && props.damageDef == DamageDefOf.Bomb)
            {
                result += props.damageAmountBase;
                for (int i = 1; i < props.explosionRadius; i++)
                {
                    result += DamageAtRadius(projectile, i) * Mathf.Pow(2, i);
                }
            }
            return result;
        }
        public static float DamageAtRadius(ThingDef projectile, int radius)
        {
            if (!projectile.projectile.explosionDamageFalloff)
            {
                return projectile.projectile.damageAmountBase;
            }
            float t = radius / projectile.projectile.explosionRadius;
            return Mathf.Max(GenMath.RoundRandom(Mathf.Lerp((float)projectile.projectile.damageAmountBase, (float)projectile.projectile.damageAmountBase * 0.2f, t)), 1);
        }
        #endregion

    }
}
