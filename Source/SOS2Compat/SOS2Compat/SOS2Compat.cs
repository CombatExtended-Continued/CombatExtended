using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CombatExtended.Loader;
using HarmonyLib;
using RimWorld;
using SaveOurShip2;
using UnityEngine;
using Verse;
using Verse.Sound;
using Vehicles;

namespace CombatExtended.Compatibility.SOS2Compat
{
    public class SOS2Compat : IModPart
    {
        private static Harmony harmony;

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
            BlockerRegistry.RegisterCheckForCollisionCallback(CheckCollision); // For Shields
            harmony = new Harmony("CombatExtended.Compatibility.SOS2Compat");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
        #region Shield Logic
        // Shield Implementation is a combination of the patch for VFE Security and SOS2 Logic
        private static HashSet<CompShipHeatShield> shields;
        private static int lastCacheTick = 0;
        private static Map lastCacheMap = null;

        private static bool CheckCollision(ProjectileCE projectile, IntVec3 cell, Thing launcher)
        {
            var map = projectile.Map;
            refreshShields(map);
            var exactPosition = projectile.ExactPosition;
            foreach (var shield in shields)
            {
                if (ShieldInterceptsProjectile(shield, projectile, launcher))
                {
                    // Succesful Intercept
                    // Replicate non-ce SOS2 shield effects
                    shield.lastInterceptAngle = projectile.DrawPos.AngleToFlat(shield.parent.Position.ToVector3());
                    shield.lastIntercepted = Find.TickManager.TicksGame;
                    if (shield.parent.Spawned)
                    {
                        FleckMaker.ThrowMicroSparks(shield.parent.DrawPos, shield.parent.Map);
                        GenExplosion.DoExplosion(projectile.Position, shield.parent.Map, 3, DefDatabase<DamageDef>.GetNamed("ShieldExplosion"), null, screenShakeFactor: 0);
                    }
                    exactPosition = BlockerRegistry.GetExactPosition(projectile.OriginIV3.ToVector3(), exactPosition, new Vector3(shield.parent.Position.x, 0, shield.parent.Position.z), shield.radius * shield.radius);
                    projectile.InterceptProjectile(shield, exactPosition, true);
                    return true;
                }
            }
            return false;
        }

        private static bool ShieldInterceptsProjectile(CompShipHeatShield shield, ProjectileCE projectile, Thing launcher)
        {
            // If shield is shut down, skip it
            if (shield.shutDown)
            {
                return false;
            }

            // Converting for distance calcs
            Vector3 shieldPos = shield.parent.Position.ToVector3Shifted();
            Vector3 launcherPos = launcher.Position.ToVector3Shifted();
            Vector3 projectilePos = projectile.Position.ToVector3Shifted();

            shieldPos.y = launcherPos.y; // We just want to check x,z distance
            // Check if shot is originating from inside the shield
            if (Vector3.Distance(shieldPos, launcherPos) < shield.radius)
            {
                // From inside shield so ignore
                return false;
            }

            shieldPos.y = projectilePos.y; // We just want to check x,z distance
            // Check if shot is inside shield radius
            if (Vector3.Distance(shieldPos, projectilePos) > shield.radius)
            {
                // outside shield radius so ignore
                return false;
            }

            // Check if shield has enough heat to block
            if (!shield.AddHeatToNetwork(CalcHeatGenerated(projectile, shield)))
            {
                // Not enough heat, break the shield using the last of the heat up
                if (shield.myNet != null)
                {
                    // Use up any last bit of heat
                    shield.AddHeatToNetwork(shield.myNet.StorageCapacity - shield.myNet.StorageUsed);
                }
                if (shield.breakComp != null)
                {
                    // Break the shield if not a vehicle
                    shield.breakComp.DoBreakdown();
                }
                else
                {
                    // Break the shield component if a vehicle
                    shield.parentVehicle.statHandler.SetComponentHealth("shieldGenerator", 0);
                    if (shield.parentVehicle.Spawned)
                    {
                        shield.parentVehicle.Map.GetComponent<ListerVehiclesRepairable>().Notify_VehicleTookDamage(shield.parentVehicle);
                    }
                }
                // Break Effects
                if (shield.parent.Spawned)
                {
                    GenExplosion.DoExplosion(shield.parent.Position, shield.parent.Map, 1.9f, DamageDefOf.Flame, shield.parent);
                }
                SoundDef.Named("EnergyShield_Broken").PlayOneShot(new TargetInfo(shield.parent));
                // Notifs
                if (shield.parent.Faction != Faction.OfPlayer)
                {
                    Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.CombatShieldBrokenEnemy"), shield.parent, MessageTypeDefOf.PositiveEvent);
                }
                else
                {
                    Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.CombatShieldBroken"), shield.parent, MessageTypeDefOf.NegativeEvent);
                }
                return false;
            }
            return true;
        }

        private static float CalcHeatGenerated(ProjectileCE projectile, CompShipHeatShield shield)
        {
            float heatGenerated = projectile.DamageAmount * 3.5f * shield.Props.heatMultiplier; // This probably needs to be adjusted for balance!
            return heatGenerated;
        }

        private static void refreshShields(Map map)
        {
            int thisTick = Find.TickManager.TicksAbs;
            if (lastCacheTick != thisTick || lastCacheMap != map)
            {
                // Cache Shields
                IEnumerable<CompShipHeatShield> ls = map.GetComponent<ShipMapComp>().Shields;
                shields = ls.ToHashSet();
                lastCacheTick = thisTick;
                lastCacheMap = map;
            }
        }
        #endregion
    }
}
