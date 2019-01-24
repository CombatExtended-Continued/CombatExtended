using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;

namespace CombatExtended
{
    public class BulletCE : ProjectileCE
    {
        private const float StunChance = 0.1f;

        private void LogImpact(Thing hitThing, out LogEntry_DamageResult logEntry)
        {
			logEntry =
				new BattleLogEntry_RangedImpact(
					launcher,
					hitThing,
					intendedTarget,
					equipmentDef,
					def,
                    null //CoverDef Missing!
                    );
            if (!(launcher is AmmoThing))
                Find.BattleLog.Add(logEntry);
        }
        
        protected override void Impact(Thing hitThing)
        {
            bool cookOff = (launcher is AmmoThing);

            Map map = base.Map;
            LogEntry_DamageResult logEntry = null;
            
            if (logMisses
                || 
                (!logMisses
                    && hitThing != null
                    && (hitThing is Pawn
                        || hitThing is Building_Turret)
                 ))
            {
                if (!cookOff)
                    LogImpact(hitThing, out logEntry);
            }
            
            if (hitThing != null)
            {
                // launcher being the pawn equipping the weapon, not the weapon itself
                int damageAmountBase = def.projectile.GetDamageAmount(1);
                DamageDefExtensionCE damDefCE = def.projectile.damageDef.GetModExtension<DamageDefExtensionCE>() ?? new DamageDefExtensionCE();
                var projectilePropsCE = (ProjectilePropertiesCE)def.projectile;

                DamageInfo dinfo = new DamageInfo(
                    def.projectile.damageDef,
                    damageAmountBase,
                    projectilePropsCE.GetArmorPenetration(1), //Armor Penetration
                    ExactRotation.eulerAngles.y,
                    launcher,
                    null,
                    def);
                
                // Set impact height
                BodyPartDepth partDepth = damDefCE != null && damDefCE.harmOnlyOutsideLayers ? BodyPartDepth.Outside : BodyPartDepth.Undefined;
                	//NOTE: ExactPosition.y isn't always Height at the point of Impact!
                BodyPartHeight partHeight = new CollisionVertical(hitThing).GetCollisionBodyHeight(ExactPosition.y);
                dinfo.SetBodyRegion(partHeight, partDepth);
                if (damDefCE != null && damDefCE.harmOnlyOutsideLayers) dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);

                //The following code excludes turrets etcetera from having cook off projectile impacts recorded in their combat log.
                //If it is necessary to add cook off to turret logs, a new BattleLogEntry_ must be created, because BattleLogEntry_DamageTaken,
                //which is the only method capable of handling cookoff and only using pawns, can not take !(hitThing is Pawn).
                if (cookOff && hitThing is Pawn)
                {
                    logEntry =
                        new BattleLogEntry_DamageTaken(
                            (Pawn)hitThing,
                            DefDatabase<RulePackDef>.GetNamed("DamageEvent_CookOff"));
                    Find.BattleLog.Add(logEntry);
                }

                // Apply primary damage
                hitThing.TakeDamage(dinfo).AssociateWithLog(logEntry);

                // Apply secondary to non-pawns (pawn secondary damage is handled in the damage worker)
                // The !(hitThing is Pawn) already excludes non-pawn cookoff projectiles from being logged, as logEntry == null
                if(!(hitThing is Pawn) && projectilePropsCE != null && !projectilePropsCE.secondaryDamage.NullOrEmpty())
                {
                    foreach(SecondaryDamage cur in projectilePropsCE.secondaryDamage)
                    {
                        if (hitThing.Destroyed) break;
                        var secDinfo = new DamageInfo(
                            cur.def,
                            cur.amount,
                            projectilePropsCE.GetArmorPenetration(1), //Armor Penetration
                            ExactRotation.eulerAngles.y,
                            launcher,
                            null,
                            def
                        );
                        hitThing.TakeDamage(secDinfo).AssociateWithLog(logEntry);
                    }
                }
            }
            else
            {
                SoundDefOf.BulletImpact_Ground.PlayOneShot(new TargetInfo(base.Position, map, false));

                //Only display a dirt/water hit for projectiles with a dropshadow
                if (base.castShadow)
                { 
                    MoteMaker.MakeStaticMote(this.ExactPosition, map, ThingDefOf.Mote_ShotHit_Dirt, 1f);
                    if (base.Position.GetTerrain(map).takeSplashes)
                    {
                        MoteMaker.MakeWaterSplash(this.ExactPosition, map, Mathf.Sqrt(def.projectile.GetDamageAmount(this.launcher)) * 1f, 4f);
                    }
                }
            }
            base.Impact(hitThing);
        }
    }
}