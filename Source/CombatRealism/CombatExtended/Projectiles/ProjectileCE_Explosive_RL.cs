using System;
using Verse;
using UnityEngine;
using RimWorld;

namespace CombatExtended
{
    public class ProjectileCE_Explosive_RL : ProjectileCE
    {
        private int ticksToDetonation;
        private int Burnticks = 3;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue<int>(ref this.ticksToDetonation, "ticksToDetonation", 0, false);
        }
        public override void Tick()
        {
            base.Tick();
            Map map = base.Map;
            if (--Burnticks == 0)
            {
                ThrowSmokeForRocketsandMortars(base.Position.ToVector3Shifted(), 1f);
                ThrowRocketExhaustFlame(base.Position.ToVector3Shifted(), 2f);
                Burnticks = 3;
            }
            if (this.ticksToDetonation > 0)
            {
                this.ticksToDetonation--;
                if (this.ticksToDetonation <= 0)
                {
                    this.Explode();
                }
            }
        }

        public override void SpawnSetup(Map map)
        {
            base.SpawnSetup(map);
            ThrowSmokeForRocketsandMortars(base.Position.ToVector3Shifted(), 4f);
            ThrowRocketExhaustFlame(base.Position.ToVector3Shifted(), 1f);
        }

        protected override void Impact(Thing hitThing)
        {
            if (this.def.projectile.explosionDelay == 0)
            {
                this.Explode();
                return;
            }
            this.landed = true;
            this.ticksToDetonation = this.def.projectile.explosionDelay;
            GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(this, this.def.projectile.damageDef, this.launcher.Faction);
        }
        protected virtual void Explode()
        {
            Map map = base.Map;
            this.Destroy(DestroyMode.Vanish);
            ProjectilePropertiesCE propsCE = def.projectile as ProjectilePropertiesCE;
            ThingDef preExplosionSpawnThingDef = this.def.projectile.preExplosionSpawnThingDef;
            float explosionSpawnChance = this.def.projectile.explosionSpawnChance;
            GenExplosion.DoExplosion(base.Position,
                map,
                this.def.projectile.explosionRadius,
                this.def.projectile.damageDef,
                this.launcher,
                this.def.projectile.soundExplode,
                this.def,
                this.equipmentDef,
                this.def.projectile.postExplosionSpawnThingDef,
                this.def.projectile.explosionSpawnChance,
                1,
                false, // propsCE == null ? false : propsCE.damageAdjacentTiles,
                preExplosionSpawnThingDef,
                this.def.projectile.explosionSpawnChance,
                1);
            ThrowBigExplode(base.Position.ToVector3Shifted() + Gen.RandomHorizontalVector(def.projectile.explosionRadius * 0.5f), map, def.projectile.explosionRadius * 0.4f);
            CompExplosiveCE comp = this.TryGetComp<CompExplosiveCE>();
            if (comp != null)
            {
                comp.Explode(launcher, this.Position, Find.VisibleMap);
            }
        }

        public static void ThrowBigExplode(Vector3 loc, Map map, float size)
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
        }

        public static void ThrowRocketExhaustFlame(Vector3 loc, float size)
        {
            IntVec3 intVec = loc.ToIntVec3();
            if (!intVec.InBounds(Find.VisibleMap))
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDefOf.Mote_ShotFlash, null);
            moteThrown.Scale = Rand.Range(1.5f, 2.5f) * size;
            moteThrown.exactRotation = Rand.Range(-0.5f, 0.5f);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(30, 40), Rand.Range(0.08f, 0.12f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), Find.VisibleMap);
        }
        public static void ThrowSmokeForRocketsandMortars(Vector3 loc, float size)
        {
            IntVec3 intVec = loc.ToIntVec3();
            if (!intVec.InBounds(Find.VisibleMap))
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDefOf.Mote_Smoke, null);
            moteThrown.Scale = Rand.Range(1.5f, 2.5f) * size;
            moteThrown.exactRotation = Rand.Range(-0.5f, 0.5f);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(30, 40), Rand.Range(0.08f, 0.12f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), Find.VisibleMap);
        }
    }
}