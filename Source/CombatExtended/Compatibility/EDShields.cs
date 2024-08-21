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
using Jaxxa.EnhancedDevelopment.Shields.Shields;


namespace CombatExtended.Compatibility
{
    class EDShields : IPatch
    {
        public static SoundDef HitSoundDef = null;


        public static List<Building> shields;

        public static int lastCacheTick = 0;
        public static Map lastCacheMap = null;

        public bool CanInstall()
        {
            if (!ModLister.HasActiveModWithName("ED-Shields"))
            {
                return false;
            }
            return true;
        }
        public void Install()
        {
            BlockerRegistry.RegisterCheckForCollisionBetweenCallback(EDShields.CheckForCollisionBetweenCallback);
            BlockerRegistry.RegisterImpactSomethingCallback(EDShields.ImpactSomethingCallback);
            BlockerRegistry.RegisterShieldZonesCallback(EDShields.ShieldZonesCallback);
            Type t = Type.GetType("Jaxxa.EnhancedDevelopment.Shields.Shields.ShieldManagerMapComp, ED-Shields");
            HitSoundDef = (SoundDef)t.GetField("HitSoundDef", BindingFlags.Static | BindingFlags.Public).GetValue(null);
        }

        public static bool CheckForCollisionBetweenCallback(ProjectileCE projectile, Vector3 from, Vector3 to)
        {
            /* Check if an active shield can block this projectile
             */
            if (projectile.def.projectile.flyOverhead)
            {
                return false;
            }
            Thing launcher = projectile.launcher;
            Map map = projectile.Map;
            Vector3 exactPosition = projectile.ExactPosition;
            IntVec3 origin = projectile.OriginIV3;
            Quaternion targetAngle = projectile.ExactRotation;
            getShields(map);

            foreach (Building building in shields)
            {
                var shield = building as Building_Shield;
                var generator = shield.GetComp<Comp_ShieldGenerator>();
                bool isActive = generator.IsActive();
                if (!isActive)
                {
                    continue;
                }
                bool blockDirect = generator.BlockDirect_Active();
                if (!blockDirect)
                {
                    continue;
                }
                int fieldRadius = (int)generator.FieldRadius_Active();
                Vector3 shieldPosition2D = new Vector3(shield.Position.x, 0, shield.Position.z);
                Vector3 nep;

                if (CE_Utility.IntersectionPoint(from, to, shieldPosition2D, fieldRadius, out Vector3[] sect, map: map, spherical: false, catchOutbound: false))
                {
                    nep = sect.OrderBy(x => (projectile.OriginIV3.ToVector3() - x).sqrMagnitude).First();
                }
                else
                {
                    continue;
                }

                Quaternion shieldProjAng = Quaternion.LookRotation(from - shieldPosition2D);
                if ((Quaternion.Angle(targetAngle, shieldProjAng) > 90))
                {
                    HitSoundDef.PlayOneShot((SoundInfo)new TargetInfo(shield.Position, map, false));

                    int damage = (projectile.def.projectile.GetDamageAmount(launcher));

                    generator.FieldIntegrity_Current -= damage;

                    exactPosition = nep;
                    FleckMakerCE.ThrowLightningGlow(exactPosition, map, 0.5f);
                    projectile.InterceptProjectile(shield, exactPosition, false);
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
            foreach (Building building in shields)
            {
                var shield = building as Building_Shield;
                var generator = shield.GetComp<Comp_ShieldGenerator>();
                bool isActive = (bool)generator.IsActive();
                if (!isActive)
                {
                    continue;
                }
                bool blockIndirect = (bool)generator.BlockIndirect_Active();
                if (!blockIndirect)
                {
                    continue;
                }
                int fieldRadius = (int)generator.FieldRadius_Active();
                int fieldRadiusSq = fieldRadius * fieldRadius;
                float DistanceSq = projectile.Position.DistanceToSquared(shield.Position) - fieldRadiusSq;
                if (DistanceSq > 0)
                {
                    continue;
                }
                HitSoundDef.PlayOneShot((SoundInfo)new TargetInfo(shield.Position, map, false));
                FleckMakerCE.ThrowLightningGlow(destination, map, 0.5f);
                int damage = (projectile.def.projectile.GetDamageAmount(launcher));
                generator.FieldIntegrity_Current -= damage;
                projectile.InterceptProjectile(shield, projectile.ExactPosition, true);
                return true;
            }
            return false;
        }


        public static void getShields(Map map)
        {
            int thisTick = Find.TickManager.TicksAbs;
            if (lastCacheTick != thisTick || lastCacheMap != map)
            {
                List<Building> buildings = map.listerBuildings.allBuildingsColonist;
                shields = buildings.Where(b => b is Building_Shield).ToList();
                lastCacheTick = thisTick;
                lastCacheMap = map;
            }
        }

        private static IEnumerable<IEnumerable<IntVec3>> ShieldZonesCallback(Thing pawnToSuppress)
        {
            Map map = pawnToSuppress.Map;
            getShields(map);
            List<IEnumerable<IntVec3>> result = new List<IEnumerable<IntVec3>>();
            foreach (Building building in shields)
            {
                var shield = building as Building_Shield;
                var generator = shield.GetComp<Comp_ShieldGenerator>();
                bool isActive = generator.IsActive();
                if (!isActive)
                {
                    continue;
                }
                bool blockDirect = generator.BlockDirect_Active();
                if (!blockDirect)
                {
                    continue;
                }
                //Is there no shields that doesn't intercept ingoing friendly projectiles?
                int fieldRadius = (int)generator.FieldRadius_Active();
                result.Add(GenRadial.RadialCellsAround(shield.Position, fieldRadius, true));
            }
            return result;
        }

    }
}
