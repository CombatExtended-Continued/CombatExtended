using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;
using HarmonyLib;
using CombatExtended.AI;

namespace CombatExtended
{
    public class BulletCE : ProjectileCE
    {
        private static RulePackDef cookOffDamageEvent = null;

        public static RulePackDef CookOff => cookOffDamageEvent ?? (cookOffDamageEvent = DefDatabase<RulePackDef>.GetNamed("DamageEvent_CookOff"));
        public virtual float DamageAmount
        {
            get
            {
                return def.projectile.GetDamageAmount(shotSpeed / def.projectile.speed);
            }
        }

        public virtual float PenetrationAmount
        {
            get
            {                
                var projectilePropsCE = (ProjectilePropertiesCE)def.projectile;
                var isSharpDmg = def.projectile.damageDef.armorCategory == DamageArmorCategoryDefOf.Sharp;
                return (isSharpDmg ? projectilePropsCE.armorPenetrationSharp : projectilePropsCE.armorPenetrationBlunt) * Mathf.Pow(shotSpeed / def.projectile.speed, 2);
            }
        }        

        private void LogImpact(Thing hitThing, out LogEntry_DamageResult logEntry)
        {
            var ed = equipmentDef ?? ThingDef.Named("Gun_Autopistol");
            logEntry =
                new BattleLogEntry_RangedImpact(
                    launcher,
                    hitThing,
                    intendedTargetThing,
                    ed,
                    def,
                    null //CoverDef Missing!
                    );
            if (!(launcher is AmmoThing))
                Find.BattleLog.Add(logEntry);
        }

        public override void Impact(Thing hitThing, bool destroyOnImpact = true)
        {
            bool cookOff = (launcher is AmmoThing);

            Map map = base.Map;
            LogEntry_DamageResult logEntry = null;

            if (!cookOff && (logMisses || hitThing is Pawn || hitThing is Building_Turret))
            {
                LogImpact(hitThing, out logEntry);
            }            
            if (hitThing != null)
            {
                // launcher being the pawn equipping the weapon, not the weapon itself
                float damageAmountBase = DamageAmount;
                var projectilePropsCE = (ProjectilePropertiesCE)def.projectile;
                var isSharpDmg = def.projectile.damageDef.armorCategory == DamageArmorCategoryDefOf.Sharp;
                var penetration = PenetrationAmount;
                var damDefCE = def.projectile.damageDef.GetModExtension<DamageDefExtensionCE>() ?? new DamageDefExtensionCE();
                var dinfo = new DamageInfo(
                          def.projectile.damageDef,
                          damageAmountBase,
                          penetration, //Armor Penetration
                          ExactRotation.eulerAngles.y,
                          launcher,
                          null,
                          def);

                // Set impact height
                BodyPartDepth partDepth = damDefCE.harmOnlyOutsideLayers ? BodyPartDepth.Outside : BodyPartDepth.Undefined;
                //NOTE: ExactPosition.y isn't always Height at the point of Impact!
                BodyPartHeight partHeight = new CollisionVertical(hitThing).GetCollisionBodyHeight(ExactPosition.y);
                dinfo.SetBodyRegion(partHeight, partDepth);
                if (damDefCE.harmOnlyOutsideLayers) dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);

                //The following code excludes turrets etcetera from having cook off projectile impacts recorded in their combat log.
                //If it is necessary to add cook off to turret logs, a new BattleLogEntry_ must be created, because BattleLogEntry_DamageTaken,
                //which is the only method capable of handling cookoff and only using pawns, can not take !(hitThing is Pawn).
                if (cookOff && hitThing is Pawn hitPawn)
                {
                    logEntry =
                        new BattleLogEntry_DamageTaken(
                            hitPawn,
                            CookOff
                            );
                    Find.BattleLog.Add(logEntry);                    
                }
                bool destroy = true;                
                try
                {                    
                    // Apply primary damage
                    DamageWorker.DamageResult result = hitThing.TakeDamage(dinfo);
                    float damageFactor = result.totalDamageDealt / damageAmountBase;
                    if (this.shotSpeed >= def.projectile.speed * 0.5f)
                    {                        
                        if (Rand.Chance(Props.overPenetrationChance))
                        {
                            destroy = false;
                            Ricochet(
                                hitThing,
                                this.shotRotation + Rand.Range(-10f, 10f),
                                this.shotAngle + Rand.Range(-1, 1) * 0.09f * this.shotAngle,
                                this.shotHeight + Rand.Range(-1, 1) * 0.15f * this.shotHeight
                            );
                            this.shotSpeed *= Rand.Range(0.3f, 0.8f);
                        }
                        if (hitThing.def.category != ThingCategory.Plant && thingToIgnore == null)
                        {                            
                            if (Rand.Chance(Props.fragmentationChance))
                            {
                                // ricochet self to be used as ref.
                                destroy = true;
                                // now launch the rest of our fragments.
                                int count = (int)((float)Rand.Range(Props.fragmentRange.start, Props.fragmentRange.end) * this.shotSpeed / def.projectile.speed * damageFactor);
                                while (count-- > 0)
                                {
                                    BulletCE bullet = ThingMaker.MakeThing(def) as BulletCE;
                                    GenSpawn.Spawn(bullet, ExactPosition.ToIntVec3(), map);
                                    bullet.thingToIgnore = hitThing;
                                    bullet.launcher = this.launcher;
                                    bullet.equipmentDef = this.equipmentDef;
                                    bullet.Launch(
                                        this.launcher,
                                        this.origin,
                                        this.shotAngle + Rand.Range(-Mathf.PI / 4f, Mathf.PI / 4f),
                                        this.shotRotation + Rand.Range(-60f, 60f),
                                        this.shotHeight + Rand.Range(-1f, 1f) * 0.15f * this.shotHeight,
                                        this.shotSpeed * Rand.Range(0.4f, 0.8f)
                                    );
                                }
                            }
                            if (Rand.Chance(Props.ricochetChance))
                            {
                                destroy = false;
                                Ricochet(
                                    hitThing,
                                    this.shotRotation + Rand.Range(-90f, 90f),
                                    this.shotAngle + Rand.Range(-Mathf.PI / 4f, Mathf.PI / 4f),
                                    this.shotHeight + Rand.Range(-0.15f, 0.15f) * this.shotHeight
                                );
                                this.shotSpeed *= (Rand.Range(0.4f, 0.9f) + damageFactor) / 2f;
                            }
                        }
                    }
                    result.AssociateWithLog(logEntry);
                    if (!(hitThing is Pawn))
                    {
                        // Apply secondary to non-pawns (pawn secondary damage is handled in the damage worker)
                        // The !(hitThing is Pawn) already excludes non-pawn cookoff projectiles from being logged, as logEntry == null
                        if (projectilePropsCE != null && !projectilePropsCE.secondaryDamage.NullOrEmpty())
                        {
                            foreach (SecondaryDamage cur in projectilePropsCE.secondaryDamage)
                            {
                                if (hitThing.Destroyed || !Rand.Chance(cur.chance)) break;

                                var secDinfo = cur.GetDinfo(dinfo);
                                hitThing.TakeDamage(secDinfo).AssociateWithLog(logEntry);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error("CombatExtended :: BulletCE impacting thing " + hitThing.LabelCap + " of def " + hitThing.def.LabelCap + " added by mod " + hitThing.def.modContentPack.Name + ". See following stacktrace for information.");
                    throw e;
                }
                finally
                {
                    base.Impact(hitThing, destroy);
                }
            }
            else
            {
                SoundDefOf.BulletImpact_Ground.PlayOneShot(new TargetInfo(base.Position, map, false));

                //Only display a dirt/water hit for projectiles with a dropshadow
                if (base.castShadow)
                {
                    FleckMaker.Static(this.ExactPosition, map, FleckDefOf.ShotHit_Dirt, 1f);
                    if (base.Position.GetTerrain(map).takeSplashes)
                    {
                        FleckMaker.WaterSplash(this.ExactPosition, map, Mathf.Sqrt(def.projectile.GetDamageAmount(this.launcher)) * 1f, 4f);
                    }
                }
                base.Impact(null, destroyOnImpact);
            }
            NotifyImpact(hitThing, map, Position);
        }

        /* Mostly imported wholesale from vanilla Bullet class,
         * except we create a temporary Bullet object for Notify_BulletImpactNearby.
         */
        private void NotifyImpact(Thing hitThing, Map map, IntVec3 position)
        {
            var vanillaBullet = GenerateVanillaBullet();
            BulletImpactData impactData = new BulletImpactData
            {
                bullet = vanillaBullet,
                hitThing = hitThing,
                impactPosition = position
            };
            hitThing?.Notify_BulletImpactNearby(impactData);

            for (int i = 0; i < 9; i++)
            {
                IntVec3 c = position + GenRadial.RadialPattern[i];
                if (c.InBounds(map))
                {
                    List<Thing> thingList = c.GetThingList(map);
                    for (int j = 0; j < thingList.Count; j++)
                    {
                        if (thingList[j] != hitThing)
                        {
                            thingList[j].Notify_BulletImpactNearby(impactData);
                        }
                        if (thingList[j] is Pawn pawn)
                            pawn.GetTacticalManager()?.Notify_BulletImpactNearby();
                    }
                }
            }
            vanillaBullet.Destroy();    //remove previously created object after notifications are sent
        }

        /* Used for creating instances of Bullet for use with Thing.Notify_BulletImpactNearby.
         * Current users are SmokepopBelt and BroadshieldPack, requiring bullet.def and bullet.Launcher.
         */

        // todo: remove when moved to publicised assembly
        private static readonly FieldInfo bulletLauncher = typeof(Bullet).GetField("launcher", BindingFlags.Instance | BindingFlags.NonPublic);

        private Bullet GenerateVanillaBullet()
        {
            var bullet = new Bullet
            {
                def = this.def,
                intendedTarget = this.intendedTargetThing,
            };

            bulletLauncher.SetValue(bullet, this.launcher);  //Bad for performance, refactor if a more efficient solution is possible
            return bullet;
        }
    }
}
