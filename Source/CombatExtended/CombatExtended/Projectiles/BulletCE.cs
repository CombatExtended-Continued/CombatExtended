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
                	//NOTE: ExactPosition.y isn't always Height at the point of Impact!
                BodyPartHeight partHeight = new CollisionVertical(hitThing).GetCollisionBodyHeight(ExactPosition.y);
                dinfo.SetBodyRegion(partHeight, partDepth);
                if (damDefCE != null && damDefCE.harmOnlyOutsideLayers) dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);

                // Apply primary damage
                hitThing.TakeDamage(dinfo);

                // Apply secondary to non-pawns (pawn secondary damage is handled in the damage worker)
                var projectilePropsCE = def.projectile as ProjectilePropertiesCE;
                if(!(hitThing is Pawn) && projectilePropsCE != null && !projectilePropsCE.secondaryDamage.NullOrEmpty())
                {
                    foreach(SecondaryDamage cur in projectilePropsCE.secondaryDamage)
                    {
                        if (hitThing.Destroyed) break;
                        var secDinfo = new DamageInfo(
                            cur.def,
                            cur.amount,
                            ExactRotation.eulerAngles.y,
                            launcher,
                            null,
                            def);
                        hitThing.TakeDamage(secDinfo);
                    }
                }
            }
            else
            {
                SoundDefOf.BulletImpactGround.PlayOneShot(new TargetInfo(base.Position, map, false));
                
                //Only display a dirt hit for projectiles with a dropshadow
                if (base.castShadow)
                	MoteMaker.MakeStaticMote(ExactPosition, map, ThingDefOf.Mote_ShotHit_Dirt, 1f);
            }
            base.Impact(hitThing);
        }
    }
}