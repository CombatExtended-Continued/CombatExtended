using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class CompProperties_ExplosiveCE : CompProperties
    {
        public float explosionDamage = -1;
        public List<ThingDefCountClass> fragments = new List<ThingDefCountClass>();
        public float fragRange = 0f;
        public float fragSpeedFactor = 1f;

        public float explosionRadius = 0f;
        public DamageDef explosionDamageDef;
        // instigator
        public SoundDef soundExplode = null;
        // projectile = parent.def
        // source = null
        public ThingDef postExplosionSpawnThingDef = null;
        public float postExplosionSpawnChance = 0;
        public int postExplosionSpawnThingCount = 1;
        public bool applyDamageToExplosionCellsNeighbors = false;
        public ThingDef preExplosionSpawnThingDef = null;
        public float preExplosionSpawnChance = 0;
        public int preExplosionSpawnThingCount = 1;

        public CompProperties_ExplosiveCE()
        {
            compClass = typeof(CompExplosiveCE);
        }
		
		public override void ResolveReferences(ThingDef parentDef)
		{
			base.ResolveReferences(parentDef);
			if (this.explosionDamageDef == null)
			{
				this.explosionDamageDef = DamageDefOf.Bomb;
			}
		}
    }
}
