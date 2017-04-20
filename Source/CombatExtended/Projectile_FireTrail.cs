using System;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    public class Projectile_FireTrail : ProjectileCR
    {
        private int TicksforAppearence = 5;
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
            if (--TicksforAppearence == 0)
            {
                Projectile_FireTrail.ThrowFireTrail(base.Position.ToVector3Shifted(), 0.5f);
                TicksforAppearence = 5;
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
            this.Destroy(DestroyMode.Vanish);
            ProjectilePropertiesCR propsCR = def.projectile as ProjectilePropertiesCR;
            ThingDef preExplosionSpawnThingDef = this.def.projectile.preExplosionSpawnThingDef;
            float explosionSpawnChance = this.def.projectile.explosionSpawnChance;
            GenExplosion.DoExplosion(base.Position,
                this.def.projectile.explosionRadius,
                this.def.projectile.damageDef,
                this.launcher,
                this.def.projectile.soundExplode,
                this.def,
                this.equipmentDef,
                this.def.projectile.postExplosionSpawnThingDef,
                this.def.projectile.explosionSpawnChance,
                1,
                propsCR == null ? false : propsCR.damageAdjacentTiles,
                preExplosionSpawnThingDef,
                this.def.projectile.explosionSpawnChance,
                1);
            CompExplosiveCR comp = this.TryGetComp<CompExplosiveCR>();
            if (comp != null)
            {
                comp.Explode(launcher);
            }
        }
        public static void ThrowFireTrail(Vector3 loc, float size)
        {
            if (!loc.ShouldSpawnMotesAt() || MoteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDef.Named("Mote_Firetrail"), null);
            moteThrown.ScaleUniform = Rand.Range(1.5f, 2.5f) * size;
            moteThrown.exactRotationRate = Rand.Range(-0.5f, 0.5f);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocityAngleSpeed((float)Rand.Range(30, 40), Rand.Range(0.008f, 0.012f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3());
        }
    }
}
