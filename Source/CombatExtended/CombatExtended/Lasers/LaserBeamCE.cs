using System;
using UnityEngine;
using RimWorld;
using Verse;
using System.Collections.Generic;
using Verse.Sound;
using CombatExtended;

namespace CombatExtended.Lasers
{
    public class LaserBeamCE : BulletCE
    {

	protected override float DamageAmount
	{
	    get
	    {
		return base.DamageAmount * DamageModifier;
	    }
	}
	
        public float DamageModifier = 1.0f;
        public new LaserBeamDefCE def => base.def as LaserBeamDefCE;

        public override void Draw()
        {

        }
        
        void TriggerEffect(EffecterDef effect, Vector3 position, Thing hitThing = null)
        {
            TriggerEffect(effect, IntVec3.FromVector3(position));
        }

        void TriggerEffect(EffecterDef effect, IntVec3 dest)
        {
            if (effect == null) return;

            var targetInfo = new TargetInfo(dest, Map, false);

            Effecter effecter = effect.Spawn();
            effecter.Trigger(targetInfo, targetInfo);
            effecter.Cleanup();
        }

        public void SpawnBeam(Vector3 a, Vector3 b)
        {
            LaserBeamGraphicCE graphic = ThingMaker.MakeThing(def.beamGraphic, null) as LaserBeamGraphicCE;
            if (graphic == null) return;
            graphic.ticksToDetonation = this.def.projectile.explosionDelay;
            graphic.projDef = def;
            graphic.Setup(launcher, a, b);
            GenSpawn.Spawn(graphic, Origin.ToIntVec3(), Map, WipeMode.Vanish);
        }

        void SpawnBeamReflections(Vector3 a, Vector3 b, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 dir = (b - a).normalized;
                Rand.PushState();
                Vector3 c = b - dir.RotatedBy(Rand.Range(-22.5f,22.5f)) * Rand.Range(1f,4f);
                Rand.PopState();

                SpawnBeam(b, c);
            }
        }
        //    public new ThingDef equipmentDef => base.equipmentDef
        public Vector3 destination => new Vector3(base.Destination.x,0, base.Destination.y);
        public Vector3 Origin => new Vector3(base.origin.x,0, base.origin.y);


        protected override void Impact(Thing hitThing)
        {
            LaserGunDef defWeapon = equipmentDef as LaserGunDef;
            Vector3 dir = (destination - Origin).normalized;
            Vector3 a = Origin + dir * (defWeapon == null ? 0.9f : defWeapon.barrelLength);
            Impact(hitThing, a);
        }

        public void Impact(Thing hitThing, Vector3 muzzle)
        {
            bool shielded = hitThing.IsShielded() && def.IsWeakToShields;

            LaserGunDef defWeapon = equipmentDef as LaserGunDef;
            Vector3 dir = (destination - muzzle).normalized;


            Vector3 b;
            if (hitThing == null)
            {
                b = destination;
            }
            else if (shielded)
            {
                b = hitThing.TrueCenter() - dir.RotatedBy(Rand.Range(-22.5f, 22.5f)) * 0.8f;
            }
            else if ((destination - hitThing.TrueCenter()).magnitude < 1)
            {
                b = destination;
            }
            else
            {
                b = hitThing.TrueCenter();
                b.x += Rand.Range(-0.5f, 0.5f);
                b.z += Rand.Range(-0.5f, 0.5f);
            }
            /*
            bool createsExplosion = this.def.projectile.explosionRadius>0f;
            if (createsExplosion)
            {
                this.Explode(hitThing, false);
                GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(this, this.def.projectile.damageDef, this.launcher.Faction);
            }
            */
            Pawn pawn = launcher as Pawn;
            IDrawnWeaponWithRotation weapon = null;
            if (pawn != null && pawn.equipment != null) weapon = pawn.equipment.Primary as IDrawnWeaponWithRotation;
            if (weapon == null) {
                Building_LaserGunCE turret = launcher as Building_LaserGunCE;
                if (turret != null) {
                    weapon = turret.Gun as IDrawnWeaponWithRotation;
                }
            }

	    if (hitThing is Pawn && shielded)
	    {
		DamageModifier *= def.shieldDamageMultiplier;
	      	SpawnBeamReflections(muzzle, b, 5);
	    }
	    base.Impact(hitThing);
            
        }


	


        
    }
}
