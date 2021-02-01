using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CombatExtended.CombatExtended.LoggerUtils;
using UnityEngine;
using Verse;
using VFESecurity;

namespace CombatExtended.Compatibility
{
    public class VanillaFurnitureExpandedShields
    {
        private static int lastCacheTick = 0;
        private static Map lastCacheMap = null;

        /// <summary>
        /// Set of shields. The type if Building because RimWorld dark magic doesn't allow using types that may not exist here.
        ///</summary>
        private static HashSet<Building> shields;
        private const string VFES_ModName = "Vanilla Furniture Expanded - Security";

        private static MethodInfo CanFunctionPropertyGetter;
        public static bool CanInstall()
        {
            if (!ModLister.HasActiveModWithName(VFES_ModName))
            {
                return false;
            }

            return true;
        }
        public static void Install()
        {
            // Only do this after we're sure that Building_Shield is a thing.
            CanFunctionPropertyGetter = typeof(Building_Shield)?.GetProperty("CanFunction", BindingFlags.Instance | BindingFlags.NonPublic)?.GetGetMethod(nonPublic: true);

            BlockerRegistry.RegisterCheckForCollisionCallback(CheckCollision);
            BlockerRegistry.RegisterImpactSomethingCallback(ImpactSomething);
        }

        private static bool CheckCollision(ProjectileCE projectile, IntVec3 cell, Thing launcher)
        {
            if (projectile.def.projectile.flyOverhead)
            {
                // All VFE shields are bullet shields. Don't block flying projectiles.
                return false;
            }

            var map = projectile.Map;
            Vector3 exactPosition = projectile.ExactPosition;

            refreshShields(map);

            foreach (var building in shields)
            {
                var shield = building as Building_Shield;
                if (!ShieldInterceptsProjectile(shield, projectile, launcher))
                {
                    continue;
                }

                if (!(projectile is ProjectileCE_Explosive))
                {
                    shield.AbsorbDamage(projectile.def.projectile.GetDamageAmount(launcher), projectile.def.projectile.damageDef, projectile.ExactRotation.eulerAngles.y);
                }
                projectile.ExactPosition = cell.ToVector3();
                return true;

            }
            return false;
        }

        private static bool ImpactSomething(ProjectileCE projectile, Thing launcher)
        {
            var map = projectile.Map;
            Vector3 exactPosition = projectile.ExactPosition;

            refreshShields(map);

            var blocked = shields.Any(building => ShieldInterceptsProjectile(building as Building_Shield, projectile, launcher));
            CELogger.Message($"Blocked {projectile}? -- {blocked}");
            return blocked;
        }

        private static bool ShieldInterceptsProjectile(Building building, ProjectileCE projectile, Thing launcher)
        {
            var shield = building as Building_Shield;
            if (!shield.active || !(bool)CanFunctionPropertyGetter.Invoke(shield, null) || shield.Energy == 0)
            {
                // Shield inactive, don't intercept.
                return false;
            }

            var shieldRadiusSquared = shield.ShieldRadius * shield.ShieldRadius;
            if (launcher.Position.DistanceToSquared(shield.Position) <= shieldRadiusSquared)
            {
                // Launcher inside shield, don't intercept.
                return false;
            }
            if (projectile.Position.DistanceToSquared(shield.Position) <= shieldRadiusSquared)
            {
                // Launcher outside, projectile inside, intercept.
                return true;
            }
            return false;
        }

        private static void refreshShields(Map map)
        {
            int thisTick = Find.TickManager.TicksAbs;
            if (lastCacheTick != thisTick || lastCacheMap != map)
            {
                // Can't use AllBuildingsColonstOfClass because type may not exist.
                shields = map.listerBuildings.allBuildingsColonist.Where(s => s is Building_Shield).ToHashSet();
                lastCacheTick = thisTick;
                lastCacheMap = map;
            }
        }
    }
}
