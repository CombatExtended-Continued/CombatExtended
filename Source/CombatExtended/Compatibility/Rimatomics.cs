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
            BlockerRegistry.RegisterImpactSomethingCallback(Rimatomics.ImpactSomethingCallback);
            BlockerRegistry.RegisterShieldZonesCallback(Rimatomics.ShieldZonesCallback);
        }

        public static bool CheckForCollisionBetweenCallback(ProjectileCE projectile, Vector3 from, Vector3 to)
        {
            Map map = projectile.Map;
            getShields(map);
            if (!found)
            {
                return false;
            }
            Vector3 exactPosition = projectile.ExactPosition;
            IntVec3 origin = projectile.OriginIV3;
            Quaternion targetAngle = projectile.ExactRotation;

            if (projectile.launcher == null)
            {
                return false;
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

                int fieldRadius = (int)shield.Radius;
                Vector3 shieldPosition2d = new Vector3(shield.parent.Position.x, 0, shield.parent.Position.z);

                if (!CE_Utility.IntersectionPoint(from, to, shieldPosition2d, fieldRadius, out Vector3[] sect, map: map, spherical: false, catchOutbound: interceptOutgoing))
                {
                    continue;
                }
                Vector3 nep = sect.OrderBy(x => (projectile.OriginIV3.ToVector3() - x).sqrMagnitude).First();
                Quaternion shieldProjAng = Quaternion.LookRotation(new Vector3(from.x - shieldPosition2d.x, 0, from.z - shieldPosition2d.z));
                var angle = Quaternion.Angle(targetAngle, shieldProjAng);
                if (angle > 90 || interceptOutgoing)
                {
                    int damage = (projectile.def.projectile.GetDamageAmount(projectile.launcher));

                    exactPosition = BlockerRegistry.GetExactPosition(origin.ToVector3(), projectile.ExactPosition, shield.parent.Position.ToVector3(), (fieldRadius - 1) * (fieldRadius - 1));
                    FleckMakerCE.ThrowLightningGlow(exactPosition, map, 0.5f);
                    projectile.ExactPosition = exactPosition;

                    Effecter effecter = new Effecter(shield.Props.interceptEffect ?? EffecterDefOf.Interceptor_BlockedProjectile);
                    effecter.Trigger(new TargetInfo(IntVec3Utility.ToIntVec3(exactPosition), shield.parent.Map, false), TargetInfo.Invalid);
                    effecter.Cleanup();
                    shield.energy -= damage * shield.EnergyLossPerDamage;
                    if (shield.energy < 0f)
                    {
                        DamageInfo dinfo = new DamageInfo(projectile.def.projectile.damageDef, (float)damage, 0f, -1f, null, null, null, 0, null, true, true);
                        shield.BreakShield(dinfo);
                    }


                    projectile.InterceptProjectile(shield, exactPosition, true);

                    return true;
                }
            }
            return false;
        }
        public static bool ImpactSomethingCallback(ProjectileCE projectile, Thing launcher)
        {
            bool flyOverhead = projectile.def.projectile.flyOverhead;
            if (!flyOverhead)
            {
                return false;
            }
            Map map = projectile.Map;
            getShields(map);
            Vector3 destination = projectile.ExactPosition;
            foreach (CompRimatomicsShield shield in shields)
            {
                if (!shield.Active || shield.ShieldState != ShieldState.Active)
                {
                    continue;
                }
                if (!GenHostility.HostileTo(projectile.launcher, shield.parent) && !shield.debugInterceptNonHostileProjectiles && !shield.Props.interceptNonHostileProjectiles)
                {
                    continue;
                }
                int fieldRadius = (int)shield.Radius;
                int fieldRadiusSq = fieldRadius * fieldRadius;
                float DistanceSq = projectile.Position.DistanceToSquared(shield.parent.Position) - fieldRadiusSq;
                if (DistanceSq > 0)
                {
                    continue;
                }

                int damage = (projectile.def.projectile.GetDamageAmount(launcher));
                Effecter effecter = new Effecter(shield.Props.interceptEffect ?? EffecterDefOf.Interceptor_BlockedProjectile);
                effecter.Trigger(new TargetInfo(projectile.Position, shield.parent.Map, false), TargetInfo.Invalid);
                effecter.Cleanup();
                shield.energy -= damage * shield.EnergyLossPerDamage;
                if (shield.energy < 0f)
                {
                    DamageInfo dinfo = new DamageInfo(projectile.def.projectile.damageDef, (float)damage, 0f, -1f, null, null, null, 0, null, true, true);
                    shield.BreakShield(dinfo);
                }
                projectile.InterceptProjectile(shield, projectile.ExactPosition, true);
                return true;
            }
            return false;
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



