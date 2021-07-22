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
        public bool IsCookingOff => numToCookOff > 0;

        #endregion

        #region Methods

        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            base.PreApplyDamage(ref dinfo, out absorbed);
            if (!absorbed && Spawned && dinfo.Def.ExternalViolenceFor(this))
            {
                if (HitPoints - dinfo.Amount > 0)
                {
                    numToCookOff += Mathf.RoundToInt(def.stackLimit * ((float)dinfo.Amount / HitPoints) * (def.smallVolume ? Rand.Range(1f, 2f) : Rand.Range(0.0f, 1f)));
                }
                //Assume CompExplosive destroys on kill
                else if (this.TryGetComp<CompExplosive>() == null || !this.TryGetComp<CompExplosive>().Props.explodeOnKilled)
                    TryDetonate(stackCount);
            }
        }

        int shouldDestroy = -1;
        public bool ShouldDestroy
        {
            get
            {
                if (shouldDestroy == -1)
                {
                    shouldDestroy = !def.IsWeapon && (AmmoDef?.tradeTags?.Contains(AmmoInjector.destroyWithAmmoDisabledTag) ?? false)
                        ? 1     //isn't a weapon AND has the tag
                        : 0;    //is a weapon or doesn't have the tag (shouldn't be destroyed)
                }
                return shouldDestroy == 1 && !AmmoUtility.IsAmmoSystemActive(AmmoDef);
            }
        }

        public override void Tick()
        {
            // Self-destruct if ammo is disabled
            if (ShouldDestroy) Destroy(DestroyMode.Vanish);

            //Calls CompExplosive _first_
            base.Tick();

            // Cook off ammo based on how much damage we've taken so far
            if (numToCookOff > 0 && Rand.Chance((float)numToCookOff / def.stackLimit))
            {
                if (TryLaunchCookOffProjectile() || TryDetonate())
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

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string inspectString = base.GetInspectString();

            if (!inspectString.NullOrEmpty())
            {
                stringBuilder.AppendLine(inspectString);
            }
            if (AmmoUtility.IsAmmoSystemActive(AmmoDef))
            {
                var count = AmmoDef?.Users.Count ?? 0;

                if (count >= 1)
                    stringBuilder.AppendLine("CE_UsedBy".Translate() + ": " + AmmoDef.Users.FirstOrDefault().LabelCap + (AmmoDef.Users.Count > 1 ? " (+" + (AmmoDef.Users.Count - 1) + ")" : ""));
            }

            return stringBuilder.ToString().TrimEndNewlines();
        }

        private bool TryDetonate(float stackCountScale = 1)
        {
            if (Find.Maps.IndexOf(Map) < 0)
                return false;

            CompExplosiveCE comp = this.TryGetComp<CompExplosiveCE>();
            var detProps = AmmoDef?.detonateProjectile?.projectile;

            if (comp != null || detProps != null)
            {
                if (Rand.Chance(Mathf.Clamp01(0.75f - Mathf.Pow(HitPoints / MaxHitPoints, 2))))
                {
                    if (comp != null)
                        comp.Explode(this, Position.ToVector3Shifted(), Map, Mathf.Pow(stackCountScale, 0.333f), null, new List<Thing>() { this });
                    else
                        this.TryGetComp<CompFragments>()?.Throw(Position.ToVector3Shifted(), Map, this); //Mathf.Pow(scale, 0.333f));

                    if (detProps != null)
                    {
                        GenExplosionCE.DoExplosion(Position, Map, detProps.explosionRadius, detProps.damageDef,
                            this, detProps.GetDamageAmount(1), GenExplosionCE.GetExplosionAP(detProps),
                            detProps.soundExplode,
                            null, def, null, detProps.postExplosionSpawnThingDef, detProps.postExplosionSpawnChance,
                            detProps.postExplosionSpawnThingCount, detProps.applyDamageToExplosionCellsNeighbors,
                            detProps.preExplosionSpawnThingDef, detProps.preExplosionSpawnChance, detProps.preExplosionSpawnThingCount,
                            detProps.explosionChanceToStartFire, detProps.explosionDamageFalloff, null, new List<Thing>() { this }, 0f, Mathf.Pow(stackCountScale, 0.333f));
                    }
                }

                return true;
            }
            return false;
        }

        private bool TryLaunchCookOffProjectile()
        {
            if (AmmoDef == null || AmmoDef.cookOffProjectile == null || Find.Maps.IndexOf(Map) < 0) return false;

            // Spawn projectile if enabled
            if (!Controller.settings.RealisticCookOff)
            {
                ProjectileCE projectile = (ProjectileCE)ThingMaker.MakeThing(AmmoDef.cookOffProjectile);
                GenSpawn.Spawn(projectile, PositionHeld, MapHeld);

                // Launch in random direction
                projectile.canTargetSelf = true;
                projectile.minCollisionDistance = 0f;
                projectile.logMisses = false;
                projectile.Launch(this,
                    new Vector2(DrawPos.x, DrawPos.z),
                    Mathf.Acos(2 * UnityEngine.Random.Range(0.5f, 1f) - 1),
                    UnityEngine.Random.Range(0, 360),
                    0.1f,
                    AmmoDef.cookOffProjectile.projectile.speed * AmmoDef.cookOffSpeed,
                    this);
            }
            // Create sound and flash effects
            if (AmmoDef.cookOffFlashScale > 0.01) FleckMaker.Static(Position, Map, FleckDefOf.ShotFlash, AmmoDef.cookOffFlashScale);
            if (AmmoDef.cookOffSound != null) AmmoDef.cookOffSound.PlayOneShot(new TargetInfo(Position, Map));
            if (AmmoDef.cookOffTailSound != null) AmmoDef.cookOffTailSound.PlayOneShotOnCamera();

            return true;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            //Such that save-reloading doesn't stop ammo cookoff
            Scribe_Values.Look(ref numToCookOff, "numToCookOff", 0);
        }
        #endregion
    }
}
