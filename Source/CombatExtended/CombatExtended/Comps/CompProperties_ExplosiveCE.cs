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
      //public float fragRange = 0f;
        public float fragSpeedFactor = 1f;

        public float explosiveRadius = 0f;
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
