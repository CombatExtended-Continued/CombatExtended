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
    class EDShields
    {
        public static SoundDef HitSoundDef = null;


        public static List<Building> shields;

        public static int lastCacheTick = 0;
        public static Map lastCacheMap = null;

        public static bool CanInstall()
        {
            if (!ModLister.HasActiveModWithName("ED-Shields"))
            {
                return false;
            }
            return true;
        }
        public static void Install()
        {
            BlockerRegistry.RegisterCheckForCollisionCallback(EDShields.CheckForCollisionCallback);
            BlockerRegistry.RegisterImpactSomethingCallback(EDShields.ImpactSomethingCallback);
            Type t = Type.GetType("Jaxxa.EnhancedDevelopment.Shields.Shields.ShieldManagerMapComp, ED-Shields");
            HitSoundDef = (SoundDef)t.GetField("HitSoundDef", BindingFlags.Static | BindingFlags.Public).GetValue(null);
        }
        public static bool CheckForCollisionCallback(ProjectileCE projectile, IntVec3 cell, Thing launcher)
        {
            /* Check if an active shield can block this projectile, we don't check if the projectile flies overhead, as those projectiles don't call this function
             */
            Map map = projectile.Map;
            Vector3 exactPosition = projectile.ExactPosition;
            IntVec3 origin = projectile.OriginIV3;
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
                int fieldRadiusSq = fieldRadius * fieldRadius;
                float DistanceSq = projectile.Position.DistanceToSquared(shield.Position) - fieldRadiusSq;
                float originDistanceSq = origin.DistanceToSquared(shield.Position) - fieldRadiusSq;
                if (DistanceSq > 0)
                {
                    continue;
                }
                if (originDistanceSq < 0)
                {
                    continue;
                }
                Vector3 shieldPosition2D = new Vector3(shield.Position.x, 0, shield.Position.z);
                Quaternion targetAngle = projectile.ExactRotation;
                Quaternion shieldProjAng = Quaternion.LookRotation(exactPosition - shieldPosition2D);
                if ((Quaternion.Angle(targetAngle, shieldProjAng) > 90))
                {

                    HitSoundDef.PlayOneShot((SoundInfo)new TargetInfo(shield.Position, map, false));


                    int damage = (projectile.def.projectile.GetDamageAmount(launcher));

                    generator.FieldIntegrity_Current -= damage;

                    exactPosition = BlockerRegistry.GetExactPosition(origin.ToVector3(), projectile.ExactPosition, shield.Position.ToVector3(), (fieldRadius - 1) * (fieldRadius - 1));
                    FleckMaker.ThrowLightningGlow(exactPosition, map, 0.5f);
                    projectile.ExactPosition = exactPosition;
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
                FleckMaker.ThrowLightningGlow(destination, map, 0.5f);
                int damage = (projectile.def.projectile.GetDamageAmount(launcher));
                generator.FieldIntegrity_Current -= damage;
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

    }
}
