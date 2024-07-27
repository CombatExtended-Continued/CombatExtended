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
    public class VehiclesCompat : IModPart
    {
        public Type GetSettingsType()
        {
            return typeof(VehicleSettings);
        }
        public IEnumerable<string> GetCompatList()
        {
            yield break;
        }
        public void PostLoad(ModContentPack content, ISettingsCE vehicleSettings)
        {
            VehicleTurret.ProjectileAngleCE = ProjectileAngleCE;
            VehicleTurret.LookupAmmosetCE = LookupAmmosetCE;
            VehicleTurret.LaunchProjectileCE = LaunchProjectileCE;
            VehicleTurret.LookupProjectileCountAndSpreadCE = LookupProjectileCountAndSpreadCE;
            VehicleTurret.NotifyShotFiredCE = NotifyShotFiredCE;
            global::CombatExtended.Compatibility.Patches.RegisterCollisionBodyFactorCallback(_GetCollisionBodyFactors);
            global::CombatExtended.Compatibility.Patches.UsedAmmoCallbacks.Add(_GetUsedAmmo);
            var harmony = new Harmony("CombatExtended.Compatibility.VehiclesCompat");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void NotifyShotFiredCE(ThingDef projectileDef, ThingDef _ammoDef, Def _ammosetDef, VehicleTurret turret, float recoil)
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
            if (projectileDef.projectile is ProjectilePropertiesCE ppce)
            {
                CE_Utility.GenerateAmmoCasings(ppce, turret.TurretLocation, turret.vehicle.Map, -turret.TurretRotation, recoil);
            }

        }

        public static Tuple<int, float> LookupProjectileCountAndSpreadCE(ThingDef _ammoDef, Def _ammosetDef, float spread)
        {
            if (_ammoDef is AmmoDef ammoDef && _ammosetDef is AmmoSetDef ammosetDef)
            {
                foreach (var al in ammosetDef.ammoTypes)
                {
                    if (al.ammo == ammoDef)
                    {
                        var projectileDef = al.projectile;
                        if (projectileDef.projectile is ProjectilePropertiesCE pprop)
                        {
                            return new Tuple<int, float>(pprop.pelletCount, spread * pprop.spreadMult);
                        }
                        break;
                    }
                }
            }
            return new Tuple<int, float>(1, spread);
        }

        public static Def LookupAmmosetCE(string defName)
        {
            var list = DefDatabase<AmmoSetDef>.AllDefs.Where(x => x.defName == defName);
            if (list.EnumerableNullOrEmpty())
            {
                Log.Error($"Combat Extended Vehicle Compat: ammoset named {defName} not found.");
                return null;
            }
            return list.First();
        }

        public static IEnumerable<ThingDef> _GetUsedAmmo()
        {
            if (Controller.settings.EnableAmmoSystem)
            {
                foreach (VehicleTurretDef vtd in DefDatabase<global::Vehicles.VehicleTurretDef>.AllDefs)
                {
                    if (vtd.GetModExtension<CETurretDataDefModExtension>() is CETurretDataDefModExtension cetddme)
                    {
                        if (cetddme.ammoSet != null)
                        {
                            AmmoSetDef asd = (AmmoSetDef)LookupAmmosetCE(cetddme.ammoSet);
                            if (Controller.settings.GenericAmmo && asd?.similarTo != null)
                            {
                                asd = asd.similarTo;
                            }
                            if (asd != null)
                            {
                                cetddme._ammoSet = asd;
                                var ammunition = vtd.ammunition = new ThingFilter();
                                vtd.genericAmmo = false;
                                vtd.chargePerAmmoCount = 1f / asd.ammoConsumedPerShot;
                                HashSet<ThingDef> allowedAmmo = (HashSet<ThingDef>)ammunition.AllowedThingDefs;

                                foreach (var al in asd.ammoTypes)
                                {
                                    allowedAmmo.Add(al.ammo);
                                    yield return al.ammo;
                                }

                                vtd.ammunition.ResolveReferences();
                            }
                        }
                    }
                }
            }
        }

        public static Vector2 ProjectileAngleCE(float speed, float range, Thing shooter, LocalTargetInfo target, Vector3 shotOrigin, bool flyOverhead, float gravity, float sway, float spread, float recoil)
        {
            // TODO: Handle cover
            var bounds = CE_Utility.GetBoundsFor(target.Thing);
            float dheight = (bounds.max.y + bounds.min.y) / 2 - shotOrigin.y;
            float shotAngle = ProjectileCE.GetShotAngle(speed, range, dheight, flyOverhead, gravity);

            float dTurretRotation = 0;
            float ticks = (float)(Find.TickManager.TicksAbs + shooter.thingIDNumber);
            dTurretRotation += sway * (float)Mathf.Sin(ticks * 0.022f);
            shotAngle += Mathf.Deg2Rad * 0.25f * sway * (float)Mathf.Sin(ticks * 0.0165f);
            double spreadDirection = Rand.Value * Math.PI * 2;
            double randomSpread = Rand.Value * spread;
            shotAngle += (float)(randomSpread * Math.Sin(spreadDirection) + recoil);
            dTurretRotation += (float)(randomSpread * Math.Cos(spreadDirection));
            return new Vector2(dTurretRotation, shotAngle);
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

            ProjectilePropertiesCE pprop = projectileDef.projectile as ProjectilePropertiesCE;
            bool instant = false;
            float spreadDegrees = 0;
            float aperatureSize = 0.03f;
            // Hard coded as a super high max range - TODO: change in 1.6 to pass the range from the turret to this function.
            // Should also update ProjectileCE.RayCast to not need a VerbPropertiesCE input just a float for range (Since thats all its used for).
            VerbPropertiesCE verbPropsRange = new VerbPropertiesCE
            {
                range = 1000
            };
            if (pprop != null)
            {
                instant = pprop.isInstant;
            }
            if (instant)
            {
                projectile.RayCast(
                    vehicle,
                    verbPropsRange,
                    origin,
                    shotAngle,
                    shotRotation,
                    shotHeight,
                    shotSpeed,
                    spreadDegrees,
                    aperatureSize,
                    vehicle);
            }
            else
            {
                projectile.Launch(
                    vehicle,
                    origin,
                    shotAngle,
                    shotRotation,
                    shotHeight,
                    shotSpeed,
                    vehicle);
            }
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
