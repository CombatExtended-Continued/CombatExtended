using System;
using Verse;
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
            Scribe_Values.LookValue<int>(ref this.ticksToDetonation, "ticksToDetonation", 0, false);
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
                propsCE == null ? false : propsCE.damageAdjacentTiles,
                preExplosionSpawnThingDef,
                explosionSpawnChance,
                1);

            if (map != null && base.Position.IsValid)
            {
                ThrowBigExplode(base.Position.ToVector3Shifted() + Gen.RandomHorizontalVector(def.projectile.explosionRadius * 0.5f), base.Map, def.projectile.explosionRadius * 0.4f);
            }

            CompExplosiveCE comp = this.TryGetComp<CompExplosiveCE>();
            if (comp != null && launcher != null && Position.IsValid)
            {
                comp.Explode(launcher, Position, Find.VisibleMap);
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
    }
}