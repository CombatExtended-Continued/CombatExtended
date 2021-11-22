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

        public float mass = 1.0f;
        public AmmoCategoryDef ammoClass;        

        public static RulePackDef CookOff => cookOffDamageEvent ?? (cookOffDamageEvent = DefDatabase<RulePackDef>.GetNamed("DamageEvent_CookOff"));        
        public virtual float DamageAmount
        {
            get
            {
                float speedFactor = shotSpeed / def.projectile.speed;                
                /*
                 * Calculate damage
                 */
                float damage;
                if (def.projectile.damageDef.armorCategory != DamageArmorCategoryDefOf.Sharp)
                {
                    // Get blunt damage using this equation
                    //
                    // blunt damage = Mathf.Pow(Props.armorPenetrationBlunt * 10000, 1 / 3f) / 10
                    damage = Props.armorPenetrationBlunt;
                    damage = damage * mass * (speedFactor * speedFactor);
                    damage = Mathf.Pow(damage * 10000, 1f / 3f) / 10;
                }
                else
                {
                    float KE = Props.armorPenetrationBlunt;
                    float ammoTypeFactor = Mathf.Pow(Props.GetDamageAmount(1) / 0.579f, 1 / 0.346f) / KE;
                    // Get Shap damage using this eqaution
                    //
                    // sharp damage = 0.579*((0.5 * m * v^2) * ammo_type_factor)^(0.346)
                    damage = 0.579f * Mathf.Pow((KE * mass * (speedFactor * speedFactor)) * ammoTypeFactor, 0.346f);
                }
                return damage;
            }
        }        
        public virtual float PenetrationAmount
        {
            get
            {
                float speedFactor = shotSpeed / def.projectile.speed;                
                /*
                 * Calculate armor penetration
                 */
                float penetration;
                if (def.projectile.damageDef.armorCategory != DamageArmorCategoryDefOf.Sharp)
                {
                    // Get blunt PenetrationAmount using this equation
                    //
                    // Blunt PenetrationAmount = (kinetic energy) = 0.5 * m * v^2
                    penetration = Props.armorPenetrationBlunt;
                    penetration = penetration * mass * (speedFactor * speedFactor);
                }
                else
                {
                    // Get Sharpe PenetrationAmount using this equation
                    //
                    // Sharpe penetration = YOLO * shotSpeedFactor ^ 3                    
                    penetration = Props.armorPenetrationSharp * speedFactor * speedFactor * speedFactor;
                }
                return penetration;
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

        public override void Impact(Thing hitThing)
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
                try
                {
                    // Apply primary damage
                    DamageWorker.DamageResult damageResult = hitThing.TakeDamage(dinfo);                                                        
                    // to prevent associating errors related to grammer
                    if (launcher != null)
                    {
                        damageResult.AssociateWithLog(logEntry);
                    }
                    // Apply secondary to non-pawns (pawn secondary damage is handled in the damage worker)
                    // The !(hitThing is Pawn) already excludes non-pawn cookoff projectiles from being logged, as logEntry == null
                    if (!(hitThing is Pawn) && projectilePropsCE != null && !projectilePropsCE.secondaryDamage.NullOrEmpty())
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
                    base.Impact(hitThing);
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
                base.Impact(null);
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
