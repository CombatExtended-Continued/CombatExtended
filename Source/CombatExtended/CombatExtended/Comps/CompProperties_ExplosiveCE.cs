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
        public List<ThingCountClass> fragments = new List<ThingCountClass>();
        public float fragRange = 0f;
        public float fragSpeedFactor = 1f;

        public float explosionRadius = 0f;
        public DamageDef explosionDamageDef = DamageDefOf.Bomb;
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
    }
}
