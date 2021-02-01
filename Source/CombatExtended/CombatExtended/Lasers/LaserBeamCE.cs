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
            if (hitThing == null)
            {
                TriggerEffect(def.explosionEffect, b);
                bool flag2 = this.def.causefireChance > 0f && Rand.Chance(this.def.causefireChance);
                if (flag2)
                {
                    FireUtility.TryStartFireIn(b.ToIntVec3(), pawn.Map, 0.01f);
                }
            }
            else
            {

                if (hitThing is Pawn && shielded)
                {
                //    weaponDamageMultiplier *= def.shieldDamageMultiplier;

                    SpawnBeamReflections(muzzle, b, 5);
                }

                Rand.PushState();
                bool flag2 = this.def.causefireChance > 0f && Rand.Chance(this.def.causefireChance);
                Rand.PopState();
                if (flag2)
                {
                    hitThing.TryAttachFire(0.01f);
                }
                TriggerEffect(def.explosionEffect, b, hitThing);
            }
            if (def.HediffToAdd!=null)
            {
                AddedEffect(hitThing);
            }
            Map map = base.Map;
//            base.Impact(hitThing);
            if (this.equipmentDef==null)
            {
                this.equipmentDef = this.launcher.def;
            }
            BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = new BattleLogEntry_RangedImpact(this.launcher, hitThing, this.intendedTarget, this.equipmentDef, this.def, null);
            Find.BattleLog.Add(battleLogEntry_RangedImpact);
            if (hitThing != null)
            {
                DamageDef damageDef = this.def.projectile.damageDef;
                float amount = DamageAmount;
                float armorPenetration = ArmorPenetration;
                float y = this.ExactRotation.eulerAngles.y;
                Thing launcher = this.launcher;
                ThingDef equipmentDef = this.equipmentDef;
                DamageInfo dinfo = new DamageInfo(damageDef, amount, armorPenetration, y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, this.intendedTarget);
                hitThing.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);
                Pawn hitPawn = hitThing as Pawn;
                if (hitPawn != null && hitPawn.stances != null && hitPawn.BodySize <= this.def.projectile.StoppingPower + 0.001f)
                {
                    hitPawn.stances.StaggerFor(95);
                }
            }
            else
            {
                SoundDefOf.BulletImpact_Ground.PlayOneShot(new TargetInfo(base.Position, map, false));
                MoteMaker.MakeStaticMote(this.ExactPosition, map, ThingDefOf.Mote_ShotHit_Dirt, 1f);
                if (base.Position.GetTerrain(map).takeSplashes)
                {
                    MoteMaker.MakeWaterSplash(this.ExactPosition, map, Mathf.Sqrt((float)this.def.projectile.GetDamageAmount(1f, null) * DamageModifier) * 1f, 4f);
                }
            }
        }

        public float DamageAmount
        {
            get
            {
                return this.def.projectile.GetDamageAmount(1f, null) * DamageModifier;
            }
        }

        public float ArmorPenetration
        {
            get
            {
                ProjectilePropertiesCE projectilePropertiesCE = (ProjectilePropertiesCE)this.def.projectile;
                return (this.def.projectile.damageDef.armorCategory == DamageArmorCategoryDefOf.Sharp) ? projectilePropertiesCE.armorPenetrationSharp : projectilePropertiesCE.armorPenetrationBlunt;
            }
        }

        // Token: 0x060000FB RID: 251 RVA: 0x00009248 File Offset: 0x00007448
        protected virtual void Explode(Thing hitThing, bool destroy = false)
        {
            Map map = base.Map;
            IntVec3 intVec = (hitThing != null) ? hitThing.PositionHeld : this.destination.ToIntVec3();
            if (destroy)
            {
                this.Destroy(DestroyMode.Vanish);
            }
            bool flag = this.def.projectile.explosionEffect != null;
            if (flag)
            {
                Effecter effecter = this.def.projectile.explosionEffect.Spawn();
                effecter.Trigger(new TargetInfo(intVec, map, false), new TargetInfo(intVec, map, false));
                effecter.Cleanup();
            }
            IntVec3 center = intVec;
            Map map2 = map;
            float explosionRadius = this.def.projectile.explosionRadius;
            DamageDef damageDef = this.def.projectile.damageDef;
            Thing launcher = this.launcher;
            int damageAmount = (int)(this.def.projectile.GetDamageAmount(1f, null) * DamageModifier);
            SoundDef soundExplode = this.def.projectile.soundExplode;
            ThingDef equipmentDef = this.equipmentDef;
            ThingDef def = this.def;
            ThingDef postExplosionSpawnThingDef = this.def.projectile.postExplosionSpawnThingDef;
            float postExplosionSpawnChance = this.def.projectile.postExplosionSpawnChance;
            int postExplosionSpawnThingCount = this.def.projectile.postExplosionSpawnThingCount;
            ThingDef preExplosionSpawnThingDef = this.def.projectile.preExplosionSpawnThingDef;
            GenExplosion.DoExplosion(center, map2, explosionRadius, damageDef, launcher, damageAmount, 0f, soundExplode, equipmentDef, def, null, postExplosionSpawnThingDef, postExplosionSpawnChance, postExplosionSpawnThingCount, this.def.projectile.applyDamageToExplosionCellsNeighbors, preExplosionSpawnThingDef, this.def.projectile.preExplosionSpawnChance, this.def.projectile.preExplosionSpawnThingCount, this.def.projectile.explosionChanceToStartFire, this.def.projectile.explosionDamageFalloff);
        }


        protected void AddedEffect(Thing hitThing)
        {
            if (def != null && hitThing != null && hitThing is Pawn hitPawn)
            {
                Rand.PushState();
                var rand = Rand.Value; // This is a random percentage between 0% and 100%
                Rand.PopState();
                StatDef ResistHediffStat = def.ResistHediffStat;
                float AddHediffChance = def.AddHediffChance;
                float ResistHediffChance = def.ResistHediffChance;
                if (def.CanResistHediff == true)
                {
                    /*
                    if (Def.ResistHediffChance!=0)
                    {
                        rand = rand + Def.ResistHediffChance;
                    }
                    else */
                    if (def.ResistHediffStat != null)
                    {
                        ResistHediffChance = hitPawn.GetStatValue(ResistHediffStat, true);
                    }
                    AddHediffChance = AddHediffChance * ResistHediffChance;
                }

                if (rand <= AddHediffChance) // If the percentage falls under the chance, success!
                {

                    var effectOnPawn = hitPawn?.health?.hediffSet?.GetFirstHediffOfDef(def.HediffToAdd);
                    Rand.PushState();
                    var randomSeverity = Rand.Range(0.15f, 0.30f);
                    Rand.PopState();
                    if (effectOnPawn != null)
                    {
                        //If they already have plague, add a random range to its severity.
                        //If severity reaches 1.0f, or 100%, plague kills the target.
                        effectOnPawn.Severity += randomSeverity;
                    }
                    else
                    {
                        //These three lines create a new health differential or Hediff,
                        //put them on the character, and increase its severity by a random amount.
                        Hediff hediff = HediffMaker.MakeHediff(def.HediffToAdd, hitPawn, null);
                        hediff.Severity = randomSeverity;
                        hitPawn.health.AddHediff(hediff, null, null);
                    }
                }
                else //failure!
                {

                    /*
                    MoteMaker.ThrowText(hitThing.PositionHeld.ToVector3(), hitThing.MapHeld, "TST_PlagueBullet_FailureMote".Translate(Def.AddHediffChance), 12f);
                    */
                }
            }
        }
    }
}
