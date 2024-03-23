using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CombatExtended.CombatExtended.LoggerUtils;
using HarmonyLib;
using UnityEngine;
using Verse;
using VFESecurity;

namespace CombatExtended.Compatibility
{
    public class VanillaFurnitureExpandedSecurity : IPatch
    {
        private static int lastCacheTick = 0;
        private static Map lastCacheMap = null;

        /// <summary>
        /// Set of shields. The type is Building because RimWorld dark magic doesn't allow using types that may not exist here.
        ///</summary>
        private static HashSet<Building> shields;
        private const string VFES_ModName = "Vanilla Furniture Expanded - Security";

        public bool CanInstall()
        {
            if (!ModLister.HasActiveModWithName(VFES_ModName))
            {
                return false;
            }

            return true;
        }
        public void Install()
        {
            BlockerRegistry.RegisterCheckForCollisionBetweenCallback(CheckCollisionBetween);
            BlockerRegistry.RegisterShieldZonesCallback(ShieldZones);
        }


        public IEnumerable<string> GetCompatList()
        {
            yield break;
        }
        private IEnumerable<IEnumerable<IntVec3>> ShieldZones(Thing arg)
        {
            refreshShields(arg.Map);
            return shields.Where(x => IsActive(x)).Select(x => GenRadial.RadialCellsAround(x.Position, Radius(x), false));
        }

        private IEnumerable<(Vector3 IntersectionPos, Action OnIntersection)> CheckCollisionBetween(ProjectileCE projectile, Vector3 from, Vector3 to)
        {
            if (projectile.def.projectile.flyOverhead)
            {
                yield break;
            }
            refreshShields(projectile.Map);
            foreach (var shield in shields)
            {
                if (!IsActive(shield))
                {
                    continue;
                }

                if (CE_Utility.IntersectionPoint(from, to, ShieldPos(shield), Radius(shield), out var sect, false, map: projectile.Map))
                {
                    var exactPosition = sect.OrderBy(x => (projectile.OriginIV3.ToVector3() - x).sqrMagnitude).First();
                    yield return (exactPosition, () => OnIntercepted(projectile, shield, exactPosition));

                }
            }
        }
        private static void refreshShields(Map map)
        {
            int thisTick = Find.TickManager.TicksAbs;
            if (lastCacheTick != thisTick || lastCacheMap != map)
            {
                // Can't use AllBuildingsColonstOfClass because type may not exist.

                IEnumerable<Building> ls = map.GetComponent<ListerThingsExtended>().listerShieldGens;
                shields = ls.ToHashSet();
                lastCacheTick = thisTick;
                lastCacheMap = map;
            }
        }
        private static void OnIntercepted(ProjectileCE projectile, Building building, Vector3 exactPosition)
        {
            var interceptor = building as Building_Shield;
            projectile.ExactPosition = exactPosition;

            projectile.landed = true;
            FleckMakerCE.ThrowLightningGlow(exactPosition, building.Map, 0.5f);
            projectile.InterceptProjectile(interceptor, projectile.ExactPosition, true);
            interceptor.AbsorbDamage(projectile.DamageAmount, projectile.def.projectile.damageDef, projectile.launcher);
        }
        private static bool IsActive(Building building)
        {
            if (building is Building_Shield shield)
            {
                return shield.active && shield.CanFunction && shield.Energy > 0f;
            }
            return false;
        }
        private float Radius(Building building)
        {
            if (building is Building_Shield shield)
            {
                float size = shield.ShieldRadius * 2 * Mathf.Lerp(0.9f, 1.1f, shield.Energy / shield.MaxEnergy);

                int ticksSinceAbsorbDamage = Find.TickManager.TicksGame - shield.lastAbsorbDamageTick;
                if (ticksSinceAbsorbDamage < 8)
                {
                    float sizeMod = (8 - ticksSinceAbsorbDamage) / 8f * 0.05f;
                    size -= sizeMod;
                }
                size *= (256f - 15f) / 256f;//VFEs use default shield texture that have 8 px offset from left and 7 from right (up and down similar)
                return size / 2;
            }
            return 0f;
        }
        private Vector3 ShieldPos(Building building)
        {
            if (building is Building_Shield shield)
            {
                Vector3 shieldPos = shield.Position.ToVector3Shifted();
                shieldPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

                int ticksSinceAbsorbDamage = Find.TickManager.TicksGame - shield.lastAbsorbDamageTick;
                if (ticksSinceAbsorbDamage < 8)
                {
                    float sizeMod = (8 - ticksSinceAbsorbDamage) / 8f * 0.05f;
                    shieldPos += shield.impactAngleVect * sizeMod;
                }
                return shieldPos;
            }
            return Vector3.negativeInfinity;
        }
    }
}
