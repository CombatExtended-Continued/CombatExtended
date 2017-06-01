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

        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            if (hitThing != null)
            {
                int damageAmountBase = def.projectile.damageAmountBase;
                ThingDef equipmentDef = this.equipmentDef;
                DamageDefExtensionCE damDefCE = def.projectile.damageDef.GetModExtension<DamageDefExtensionCE>() ?? new DamageDefExtensionCE();

                DamageInfo dinfo = new DamageInfo(
                    def.projectile.damageDef,
                    damageAmountBase,
                    ExactRotation.eulerAngles.y,
                    launcher,
                    null,
                    def);
                
                // Set impact height
                BodyPartDepth partDepth = damDefCE != null && damDefCE.harmOnlyOutsideLayers ? BodyPartDepth.Outside : BodyPartDepth.Undefined;
                BodyPartHeight partHeight = new CollisionVertical(hitThing).GetCollisionBodyHeight(Height);
                dinfo.SetBodyRegion(partHeight, partDepth);
                if (damDefCE != null && damDefCE.harmOnlyOutsideLayers) dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);

                ProjectilePropertiesCE propsCE = def.projectile as ProjectilePropertiesCE;
                if (propsCE != null && !propsCE.secondaryDamage.NullOrEmpty())
                {
                    // Get the correct body part
                    Pawn pawn = hitThing as Pawn;
                    if (pawn != null && def.projectile.damageDef.workerClass == typeof(DamageWorker_AddInjuryCE))
                    {
                        //BodyPartRecord exactPartFromDamageInfo = DamageWorker_AddInjuryCE.GetExactPartFromDamageInfo(dinfo, pawn);
                        dinfo = new DamageInfo(
                            dinfo.Def,
                            dinfo.Amount,
                            dinfo.Angle,
                            dinfo.Instigator,
                            DamageWorker_AddInjuryCE.GetExactPartFromDamageInfo(dinfo, pawn),
                            dinfo.WeaponGear);
                    }
                    List<DamageInfo> dinfoList = new List<DamageInfo>() { dinfo };
                    foreach (SecondaryDamage secDamage in propsCE.secondaryDamage)
                    {
                        dinfoList.Add(new DamageInfo(
                            secDamage.def,
                            secDamage.amount,
                            dinfo.Angle,
                            dinfo.Instigator,
                            dinfo.ForceHitPart,
                            dinfo.WeaponGear));
                    }
                    foreach (DamageInfo curDinfo in dinfoList)
                    {
                        hitThing.TakeDamage(curDinfo);
                    }
                }
                else
                {
                    hitThing.TakeDamage(dinfo);
                }
            }
            else
            {
                SoundDefOf.BulletImpactGround.PlayOneShot(new TargetInfo(base.Position, map, false));
                MoteMaker.MakeStaticMote(ExactPosition, map, ThingDefOf.Mote_ShotHit_Dirt, 1f);
            }
        }
    }
}