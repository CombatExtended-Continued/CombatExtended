using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Verse;
using CombatExtended.Loader;
using RimWorld;
using System.Collections.Generic;
using HarmonyLib;
using Vehicles;
using UnityEngine;


namespace CombatExtended.Compatibility.VehiclesCompat
{
    [StaticConstructorOnStartup]
    public class VehiclesCompat : IModPart
    {
        public Type GetSettingsType()
        {
            return null;
        }
        public IEnumerable<string> GetCompatList()
        {
            yield break;
        }
        public void PostLoad(ModContentPack content, ISettingsCE _)
        {
            VehicleTurret.ProjectileAngleCE = ProjectileAngleCE;
            VehicleTurret.LookupAmmosetCE = LookupAmmosetCE;
            VehicleTurret.LaunchProjectileCE = LaunchProjectileCE;
            global::CombatExtended.Compatibility.Patches.RegisterCollisionBodyFactorCallback(_GetCollisionBodyFactors);
            global::CombatExtended.Compatibility.Patches.UsedAmmoCallbacks.Add(_GetUsedAmmo);
        }

        public static Def LookupAmmosetCE(string defName)
        {
            return DefDatabase<AmmoSetDef>.AllDefs.Where(x => x.defName == defName).First();
        }

        public static IEnumerable<ThingDef> _GetUsedAmmo()
        {
            foreach (VehicleTurretDef vtd in DefDatabase<global::Vehicles.VehicleTurretDef>.AllDefs)
            {
                foreach (ThingDef td in vtd?.ammunition?.AllowedThingDefs)
                {
                    if (td is AmmoDef ad)
                    {
                        yield return td;
                    }
                }
            }
        }

        public static Vector2 ProjectileAngleCE(float speed, float range, Thing shooter, LocalTargetInfo target, Vector3 shotOrigin, bool flyOverhead, float gravity, float sway, float spread, float recoil)
        {
            // TODO: Handle cover
            var bounds = CE_Utility.GetBoundsFor(target.Thing);
            float dheight = (bounds.max.y + bounds.min.y) / 2 - shotOrigin.y;
            float sa = ProjectileCE.GetShotAngle(speed, range, dheight, flyOverhead, gravity);

            float tr = 0;
            float ticks = (float)(Find.TickManager.TicksAbs + shooter.thingIDNumber);
            tr += sway * (float)Mathf.Sin(ticks * 0.022f);
            sa += Mathf.Deg2Rad * 0.25f * sway * (float)Mathf.Sin(ticks * 0.0165f);
            double an = Rand.Value * Math.PI * 2;
            double ra = Rand.Value * spread;
            sa += (float)(ra * Math.Sin(an) + recoil);
            tr += (float)(ra * Math.Cos(an));
            return new Vector2(tr, sa);
        }

        public static object LaunchProjectileCE(ThingDef projectileDef,
                                                ThingDef _ammoDef,
                                                Def _ammosetDef,
                                                Vector2 origin,
                                                LocalTargetInfo target,
                                                VehiclePawn vehicle,
                                                float shotAngle,
                                                float shotRotation,
                                                float shotHeight,
                                                float shotSpeed)
        {
            if (_ammoDef is AmmoDef ammoDef && _ammosetDef is AmmoSetDef ammosetDef)
            {
                foreach (var al in ammosetDef.ammoTypes)
                {
                    if (al.ammo == ammoDef)
                    {
                        projectileDef = al.projectile;
                        break;
                    }
                }
            }
            else
            {
                projectileDef = projectileDef.GetProjectile();
            }
            var p = ThingMaker.MakeThing(projectileDef, null);
            ProjectileCE projectile = (ProjectileCE)p;
            GenSpawn.Spawn(projectile, vehicle.Position, vehicle.Map);
            projectile.ExactPosition = origin;
            projectile.canTargetSelf = false;
            projectile.minCollisionDistance = 1;
            projectile.intendedTarget = target;
            projectile.mount = null;
            projectile.AccuracyFactor = 1;
            projectile.Launch(
                vehicle,
                origin,
                shotAngle,
                shotRotation,
                shotHeight,
                shotSpeed,
                vehicle);
            return projectile;
        }
        private static Tuple<bool, Vector2> _GetCollisionBodyFactors(Pawn pawn)
        {
            Vector2 ret = new Vector2();
            if (pawn is VehiclePawn vehicle)
            {
                ret = new Vector2(1, vehicle.def.fillPercent);
                return new Tuple<bool, Vector2>(true, ret);
            }
            return new Tuple<bool, Vector2>(false, ret);
        }

    }
}
