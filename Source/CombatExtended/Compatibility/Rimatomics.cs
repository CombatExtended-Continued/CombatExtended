using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.Sound;
using System.Reflection.Emit;
using System;
using UnityEngine;
using RimWorld;
using Rimatomics;


namespace CombatExtended.Compatibility
{
    class Rimatomics : IPatch
    {
        public static SoundDef HitSoundDef = null;


        public static List<ThingComp> shields;
        public static bool found = false;

        public static int lastCacheTick = 0;
        public static Map lastCacheMap = null;

        public bool CanInstall()
        {
            return ModLister.HasActiveModWithName("Dubs Rimatomics");
        }
        public void Install()
        {
            BlockerRegistry.RegisterCheckForCollisionBetweenCallback(Rimatomics.CheckForCollisionBetweenCallback);
            BlockerRegistry.RegisterShieldZonesCallback(Rimatomics.ShieldZonesCallback);
        }

        public IEnumerable<string> GetCompatList()
        {
            yield break;
        }

        public static IEnumerable<(Vector3 IntersectionPos, Action OnIntersection)> CheckForCollisionBetweenCallback(ProjectileCE projectile, Vector3 from, Vector3 to)
        {
            Map map = projectile.Map;
            getShields(map);
            if (!found)
            {
                yield break;
            }

            if (projectile.launcher == null)
            {
                yield break;
            }

            foreach (ThingComp thingComp in shields)
            {
                var shield = thingComp as CompRimatomicsShield;
                if (!shield.Active || shield.ShieldState != ShieldState.Active)
                {
                    continue;
                }
                if (!GenHostility.HostileTo(projectile.launcher, shield.parent) && !shield.debugInterceptNonHostileProjectiles && !shield.Props.interceptNonHostileProjectiles)
                {
                    continue;
                }
                bool interceptOutgoing = shield.Props.interceptOutgoingProjectiles;

                float fieldRadius = shield.Radius;

                if (CE_Utility.IntersectionPoint(from, to, thingComp.parent.Position.ToVector3Shifted(), fieldRadius, out Vector3[] sect, map: map, spherical: false, catchOutbound: interceptOutgoing))
                {
                    var exactPosition = sect.OrderBy(x => (projectile.OriginIV3.ToVector3() - x).sqrMagnitude).First();
                    yield return (exactPosition, () => OnIntercepted(projectile, thingComp, exactPosition));
                    
                }
            }
        }
        private static void OnIntercepted(ProjectileCE projectile, ThingComp interceptor, Vector3 exactPosition)
        {
            CompRimatomicsShield shield = interceptor as CompRimatomicsShield;
            var lastExactPos = projectile.LastPos;
            var map = projectile.Map;
            int damage = (projectile.def.projectile.GetDamageAmount(projectile.launcher));
            
            DamageInfo dinfo = new DamageInfo(projectile.def.projectile.damageDef, damage, 0f, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true);
            shield.lastInterceptAngle = lastExactPos.AngleToFlat(shield.parent.TrueCenter());
            shield.lastInterceptTicks = Find.TickManager.TicksGame;
            if (projectile.def.projectile.damageDef == DamageDefOf.EMP && shield.Props.disarmedByEmpForTicks > 0)
            {
                shield.BreakShield(dinfo);
            }
            FleckMakerCE.ThrowLightningGlow(exactPosition, map, 0.5f);
            Effecter effecter = new Effecter(shield.Props.interceptEffect ?? EffecterDefOf.Interceptor_BlockedProjectile);
            effecter.Trigger(new TargetInfo(exactPosition.ToIntVec3(), shield.parent.Map, false), TargetInfo.Invalid, -1);
            effecter.Cleanup();
            shield.energy -= dinfo.Amount * shield.EnergyLossPerDamage;
            if (shield.energy < 0f)
            {
                shield.BreakShield(dinfo);
            }
            projectile.InterceptProjectile(shield, exactPosition, true);
        }

        private static IEnumerable<IEnumerable<IntVec3>> ShieldZonesCallback(Thing pawnToSuppress)
        {
            Map map = pawnToSuppress.Map;
            getShields(map);
            List<IEnumerable<IntVec3>> result = new List<IEnumerable<IntVec3>>();
            foreach (CompRimatomicsShield shield in shields)
            {
                if (!shield.Active || shield.ShieldState != ShieldState.Active)
                {
                    continue;
                }
                if (GenHostility.HostileTo(pawnToSuppress, shield.parent) && !shield.debugInterceptNonHostileProjectiles && !shield.Props.interceptNonHostileProjectiles)
                {
                    // Avoid hostile shields because they aren't intercepting friendly projectiles
                    continue;
                }

                int fieldRadius = (int)shield.Radius;
                result.Add(GenRadial.RadialCellsAround(shield.parent.Position, fieldRadius, true));
            }
            return result;
        }

        public static void getShields(Map map)
        {
            int thisTick = Find.TickManager.TicksAbs;
            if (lastCacheTick != thisTick || lastCacheMap != map)
            {
                found = false;
                IEnumerable<Building> buildings = map.listerBuildings.allBuildingsColonist.Where(b => b is Building_ShieldArray);
                shields = new List<ThingComp>();
                foreach (Building b in buildings)
                {
                    found = true;
                    shields.Add((b as Building_ShieldArray).CompShield);
                }

                lastCacheTick = thisTick;
                lastCacheMap = map;
            }
        }

    }
}



