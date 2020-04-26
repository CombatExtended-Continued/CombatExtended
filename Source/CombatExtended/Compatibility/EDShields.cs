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


namespace CombatExtended.Compatibility
{
    class EDShields
    {
        public static Type shieldBuildingType;
        public static Type shieldGeneratorType;
        public static MethodInfo IsActive;
        public static MethodInfo BlockDirect_Active;
        public static MethodInfo BlockIndirect_Active;
        public static MethodInfo FieldRadius;
        public static MethodInfo GetShieldGenerator;

        public static PropertyInfo FieldIntegrity_Current;

        public static readonly SoundDef HitSoundDef = SoundDef.Named("Shields_HitShield");


        public static List<Building> shields;

        public static int lastCacheTick = 0;

        public static bool CanInstall()
        {
            if (!ModLister.HasActiveModWithName("ED-Shields"))
            {
                return false;
            }
            shieldBuildingType = Type.GetType("Jaxxa.EnhancedDevelopment.Shields.Shields.Building_Shield, ED-Shields");
            shieldGeneratorType = Type.GetType("Jaxxa.EnhancedDevelopment.Shields.Shields.Comp_ShieldGenerator, ED-Shields");

            if (shieldBuildingType == null || shieldGeneratorType == null)
            {
                return false;
            }
            IsActive = shieldGeneratorType.GetMethod("IsActive", BindingFlags.Public|BindingFlags.Instance);

            BlockIndirect_Active = shieldGeneratorType.GetMethod("BlockIndirect_Active", BindingFlags.Public|BindingFlags.Instance);
            BlockDirect_Active = shieldGeneratorType.GetMethod("BlockDirect_Active", BindingFlags.Public|BindingFlags.Instance);

            FieldRadius = shieldGeneratorType.GetMethod("FieldRadius_Active", BindingFlags.Public|BindingFlags.Instance);

            FieldIntegrity_Current = shieldGeneratorType.GetProperty("FieldIntegrity_Current", BindingFlags.Public | BindingFlags.Instance);

            if (IsActive == null || BlockIndirect_Active == null || BlockDirect_Active == null || FieldRadius == null || FieldIntegrity_Current == null)
            {
                return false;
            }
            GetShieldGenerator = typeof(Building).GetMethod("GetComp", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).MakeGenericMethod(shieldGeneratorType);
            return true;
        }
        public static void Install()
        {
            BlockerRegistry.RegisterCheckForCollisionCallback(EDShields.CheckForCollisionCallback);
            BlockerRegistry.RegisterImpactSomethingCallback(EDShields.ImpactSomethingCallback);
        }
        public static bool CheckForCollisionCallback(ProjectileCE projectile, IntVec3 cell, Thing launcher)
        {
            /* Check if an active shield can block this projectile, we don't check if the projectile flies overhead, as those projectiles don't call this function
             */
            Map map = projectile.Map;
            Vector3 exactPosition = projectile.ExactPosition;
            getShields(map);

            foreach (Building shield in shields)
            {
                var generator = GetShieldGenerator.Invoke(shield, null);
                bool isActive = (bool) IsActive.Invoke(generator, null);
                if (!isActive)
                {
                    continue;
                }
                bool blockDirect = (bool) BlockDirect_Active.Invoke(generator, null);
                if (!blockDirect)
                {
                    continue;
                }
                int fieldRadius = (int) FieldRadius.Invoke(generator, null);
                float Distance = Vector3.Distance(projectile.Position.ToVector3(), shield.Position.ToVector3()) - fieldRadius;
                if (Distance > 0)
                {
                    continue;
                }
                if (Distance < -2)
                {
                    continue;
                }
                Vector3 shieldPosition2D = new Vector3(shield.Position.x, 0, shield.Position.z);
                Quaternion targetAngle = projectile.ExactRotation;
                Quaternion shieldProjAng = Quaternion.LookRotation(exactPosition - shieldPosition2D);
                if ((Quaternion.Angle(targetAngle, shieldProjAng) > 90))
                {

                    MoteMaker.ThrowLightningGlow(exactPosition, map, 0.5f);
                    HitSoundDef.PlayOneShot((SoundInfo)new TargetInfo(shield.Position, map, false));


                    int damage = (projectile.def.projectile.GetDamageAmount(launcher));
                    int fic = (int)FieldIntegrity_Current.GetValue(generator);

                    FieldIntegrity_Current.SetValue(generator, fic - damage);
                    projectile.ExactPosition = cell.ToVector3();
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
                return true;
            }
            Map map = projectile.Map;
            getShields(map);
            Vector3 destination = projectile.ExactPosition;
            foreach (Building shield in shields)
            {
                var generator = GetShieldGenerator.Invoke(shield, null);
                bool isActive = (bool) IsActive.Invoke(generator, null);
                if (!isActive)
                {
                    continue;
                }
                bool blockIndirect = (bool) BlockIndirect_Active.Invoke(generator, null);
                if (!blockIndirect)
                {
                    continue;
                }
                int fieldRadius = (int) FieldRadius.Invoke(generator, null);
                float Distance = Vector3.Distance(projectile.Position.ToVector3(), shield.Position.ToVector3()) - fieldRadius;
                if (Distance > 0)
                {
                    continue;
                }
                HitSoundDef.PlayOneShot((SoundInfo)new TargetInfo(shield.Position, map, false));
                MoteMaker.ThrowLightningGlow(destination, map, 0.5f);
                int damage = (projectile.def.projectile.GetDamageAmount(launcher));
                int fic = (int)FieldIntegrity_Current.GetValue(generator);
                FieldIntegrity_Current.SetValue(generator, fic - damage);
                return true;
            }
            return false;
        }

        public static void getShields(Map map)
        {
            int thisTick = Find.TickManager.TicksAbs;
            if (lastCacheTick != thisTick) {
                List<Building> buildings = map.listerBuildings.allBuildingsColonist;
                shields = buildings.Where(b => shieldBuildingType.IsInstanceOfType(b)).ToList();
                lastCacheTick = thisTick;
            }
        }

    }
}
