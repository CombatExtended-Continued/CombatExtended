using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace CombatExtended
{
    public class AmmoThing : ThingWithComps
    {
        private int numToCookOff;

        #region Properties

        private AmmoDef AmmoDef => def as AmmoDef;

        #endregion

        #region Methods

        public override string DescriptionFlavor
        {
            get
            {
                if (AmmoDef != null && AmmoDef.ammoClass != null)
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.AppendLine(base.DescriptionFlavor);

                    // Append ammo class description
                    stringBuilder.AppendLine("\n" + AmmoDef.ammoClass.LabelCap + ":");
                    stringBuilder.AppendLine(AmmoDef.ammoClass.description);

                    // Append guns that use this caliber
                    var users = AmmoDef.Users;
                    if (!users.NullOrEmpty())
                    {
                        stringBuilder.AppendLine("\n" + "CE_UsedBy".Translate() + ":");
                        foreach (var user in users)
                        {
                            stringBuilder.AppendLine("   -" + user.LabelCap);
                        }
                    }

                    return stringBuilder.ToString().TrimEndNewlines();
                }

                return base.DescriptionFlavor;
            }
        }

        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            base.PreApplyDamage(ref dinfo, out absorbed);
            if (!absorbed && Spawned && dinfo.Def.ExternalViolenceFor(this))
            {
                if (HitPoints - dinfo.Amount > 0)
                {
                    numToCookOff += Mathf.RoundToInt(def.stackLimit * ((float)dinfo.Amount / HitPoints) * (def.smallVolume ? Rand.Range(1f, 2f) : Rand.Range(0.0f, 1f)));
                }
                else TryDetonate(Mathf.Lerp(1, Mathf.Min(5, stackCount), stackCount / def.stackLimit));
            }
        }

        public override void Tick()
        {
            // Self-destruct if ammo is disabled
            if (!Controller.settings.EnableAmmoSystem && !def.IsWeapon) Destroy(DestroyMode.Vanish);

            base.Tick();

            // Cook off ammo based on how much damage we've taken so far
            if (numToCookOff > 0 && Rand.Chance((float)numToCookOff / def.stackLimit))
            {
                if(TryLaunchCookOffProjectile() || TryDetonate())
                {
                    // Reduce stack count
                    if (stackCount > 1)
                    {
                        numToCookOff--;
                        stackCount--;
                    }
                    else
                    {
                        numToCookOff = 0;
                        Destroy(DestroyMode.KillFinalize);
                    }
                }
            }
        }

        private bool TryDetonate(float scale = 1)
        {
            CompExplosiveCE comp = this.TryGetComp<CompExplosiveCE>();
            if (comp != null)
            {
            	if(Rand.Chance(Mathf.Clamp01(0.75f - Mathf.Pow(HitPoints / MaxHitPoints, 2)))) comp.Explode(this, Position.ToVector3Shifted(), Map, scale);
                return true;
            }
            return false;
        }

        private bool TryLaunchCookOffProjectile()
        {
            if (AmmoDef.cookOffProjectile == null) return false;

            // Spawn projectile if enabled
            if (!Controller.settings.RealisticCookOff)
            {
                ProjectileCE projectile = (ProjectileCE)ThingMaker.MakeThing(AmmoDef.cookOffProjectile);
                GenSpawn.Spawn(projectile, Position, Map);

                // Launch in random direction
                projectile.canTargetSelf = true;
                projectile.minCollisionSqr = 0f;
                projectile.logMisses = false;
                projectile.Launch(this,
                    new Vector2(DrawPos.x, DrawPos.z),
                    UnityEngine.Random.Range(0, Mathf.PI / 2f),
                    UnityEngine.Random.Range(0, 360),
                    0.1f,
                    AmmoDef.cookOffProjectile.projectile.speed * AmmoDef.cookOffSpeed,
                    this);
            }
            // Create sound and flash effects
            if (AmmoDef.cookOffFlashScale > 0.01) MoteMaker.MakeStaticMote(Position, Map, ThingDefOf.Mote_ShotFlash, AmmoDef.cookOffFlashScale);
            if (AmmoDef.cookOffSound != null) AmmoDef.cookOffSound.PlayOneShot(new TargetInfo(Position, Map));
            if (AmmoDef.cookOffTailSound != null) AmmoDef.cookOffTailSound.PlayOneShotOnCamera();

            return true;
        }

        #endregion
    }
}
