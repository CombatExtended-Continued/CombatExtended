using System;
using Verse;
using RimWorld;
using UnityEngine;

namespace CombatExtended
{
    //Cloned from vanilla, completely unmodified
    public class ProjectileCE_Explosive : ProjectileCE
    {
        private int ticksToDetonation;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.ticksToDetonation, "ticksToDetonation", 0, false);
        }
        public override void Tick()
        {
            base.Tick();
            if (this.ticksToDetonation > 0)
            {
                this.ticksToDetonation--;
                if (this.ticksToDetonation <= 0)
                {
                    this.Explode();
                }
            }
        }
        protected override void Impact(Thing hitThing)
        {
            if (def.projectile.explosionDelay == 0)
            {
                Explode();
                return;
            }
            landed = true;
            ticksToDetonation = def.projectile.explosionDelay;
            GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(this, this.def.projectile.damageDef, this.launcher?.Faction);
        }
        protected virtual void Explode()
        {
            ExplosionCE explosion = GenSpawn.Spawn(CE_ThingDefOf.ExplosionCE, ExactPosition.ToIntVec3(), Map) as ExplosionCE;
            explosion.height = ExactPosition.y;
            explosion.radius = def.projectile.explosionRadius;
            explosion.damType = def.projectile.damageDef;
            explosion.instigator = launcher;
            explosion.damAmount = def.projectile.GetDamageAmount(CE_Utility.GetWeaponFromLauncher(launcher));
            explosion.weapon = equipmentDef;
            explosion.projectile = def;
            explosion.preExplosionSpawnThingDef = def.projectile.preExplosionSpawnThingDef;
            explosion.preExplosionSpawnChance = def.projectile.preExplosionSpawnChance;
            explosion.preExplosionSpawnThingCount = def.projectile.preExplosionSpawnThingCount;
            explosion.postExplosionSpawnThingDef = def.projectile.postExplosionSpawnThingDef;
            explosion.postExplosionSpawnChance = def.projectile.postExplosionSpawnChance;
            explosion.postExplosionSpawnThingCount = def.projectile.postExplosionSpawnThingCount;
            explosion.applyDamageToExplosionCellsNeighbors = def.projectile.applyDamageToExplosionCellsNeighbors;
            explosion.chanceToStartFire = def.projectile.explosionChanceToStartFire;
            explosion.damageFalloff = def.projectile.explosionDamageFalloff;
            explosion.StartExplosion(def.projectile.soundExplode);

            //This code was disabled because it didn't run under previous circumstances. Could be enabled if necessary
            /*
            if (map != null && base.ExactPosition.ToIntVec3().IsValid)
            {
                ThrowBigExplode(base.ExactPosition + Gen.RandomHorizontalVector(def.projectile.explosionRadius * 0.5f), base.Map, def.projectile.explosionRadius * 0.4f);
            }
            */

            base.Impact(null); // base.Impact() handles this.Destroy() and comp.Explode()
        }

      /*public static void ThrowBigExplode(Vector3 loc, Map map, float size)
        {
            if (!loc.ShouldSpawnMotesAt(map))
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDef.Named("Mote_BigExplode"), null);
            moteThrown.Scale = Rand.Range(5f, 6f) * size;
            moteThrown.exactRotation = Rand.Range(0f, 0f);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(6, 8), Rand.Range(0.002f, 0.003f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }*/
    }
}
