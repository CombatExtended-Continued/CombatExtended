using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    /// <summary>
    /// Version of CompPropertes_Explosive that does not use the wicker
    /// </summary>
    public class CompProperties_ExplosiveCE : CompProperties
    {
        public float damageAmountBase = -1;
        public List<ThingDefCountClass> fragments = new List<ThingDefCountClass>();
        public float fragSpeedFactor = 1f;

        /// <summary>Default value 1.9f as for CompProperties_Explosive</summary>
        public float explosiveRadius = 1.9f;
        public DamageDef explosiveDamageType;
        // instigator
        public SoundDef explosionSound = null;
        // projectile = parent.def
        // source = null
        public ThingDef postExplosionSpawnThingDef = null;
        public float postExplosionSpawnChance = 0;
        public int postExplosionSpawnThingCount = 1;
        public bool applyDamageToExplosionCellsNeighbors = false;
        public ThingDef preExplosionSpawnThingDef = null;
        public float preExplosionSpawnChance = 0;
        public int preExplosionSpawnThingCount = 1;
        public bool damageFalloff = true;
        public float chanceToStartFire;

        public CompProperties_ExplosiveCE()
        {
            compClass = typeof(CompExplosiveCE);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (var i in base.ConfigErrors(parentDef))
                yield return i;

            if (explosiveRadius <= 0f)
                yield return "explosiveRadius smaller or equal to zero, this explosion cannot occur";

            if (parentDef.tickerType != TickerType.Normal)
                yield return "CompExplosiveCE requires Normal ticker type";

            if (fragments.Any())
                yield return "fragments is removed from CompExplosiveCE, please use CombatExtended.CompFragments instead";
        }

        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
            if (this.explosiveDamageType == null)
            {
                this.explosiveDamageType = DamageDefOf.Bomb;
            }
        }
    }
}
