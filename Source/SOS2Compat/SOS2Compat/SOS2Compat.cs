using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CombatExtended.Loader;
using HarmonyLib;
using RimWorld;
using SaveOurShip2;
using UnityEngine;
using Verse;
using Verse.Sound;
using Vehicles;
using static RimWorld.BaseGen.SymbolResolver_BasePart_Outdoors_Division_Grid;
using Verse.Noise;

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
            BlockerRegistry.RegisterCheckForCollisionBetweenCallback(CheckCollisionBetween); // For Shields
            BlockerRegistry.RegisterShieldZonesCallback(ShieldZonesCallback);
            harmony = new Harmony("CombatExtended.Compatibility.SOS2Compat");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
        #region Shield Logic
        // Shield Implementation is a combination of the patch for VFE Security and SOS2 Logic
        private static HashSet<CompShipHeatShield> shields;
        private static int lastCacheTick = 0;
        private static Map lastCacheMap = null;

        private IEnumerable<(Vector3 IntersectionPos, Action OnIntersection)> CheckCollisionBetween(ProjectileCE projectile, Vector3 from, Vector3 to)
        {
            var map = projectile.Map;
            refreshShields(map);
            var exactPosition = projectile.ExactPosition;
            foreach (var shield in shields)
            {
                // If shield is shut down, skip it
                if (shield.shutDown)
                {
                    continue;
                }

                Vector3 shieldPosition = shield.parent.Position.ToVector3Shifted();
                Vector3 lastExactPos = projectile.LastPos.Yto0();
                var newExactPos = projectile.ExactPosition.Yto0();
                Vector3[] intersectionPoints;
                if (!CE_Utility.IntersectionPoint(lastExactPos, newExactPos, shieldPosition, shield.radius, out intersectionPoints, false))
                {
                    continue;
                }
                newExactPos = intersectionPoints.OrderBy(x => (projectile.OriginIV3.ToVector3() - x).sqrMagnitude).First();
                yield return (newExactPos, () => OnIntercepted(projectile, shield, newExactPos));
            }

        }
        private void OnIntercepted(ProjectileCE projectile, ThingComp comp, Vector3 newExactPos)
        {
            var shield = comp as CompShipHeatShield;
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
                projectile.CheckForCollisionBetween(newExactPos, projectile.ExactPosition);
            }
            else
            {
                shield.lastInterceptAngle = newExactPos.AngleToFlat(shield.parent.Position.ToVector3());
                shield.lastIntercepted = Find.TickManager.TicksGame;

                if (shield.parent.Spawned)
                {
                    SoundDef.Named("Interceptor_BlockProjectile").PlayOneShot((SoundInfo)new TargetInfo(shield.parent.Position, projectile.Map, false));
                    FleckMaker.ThrowMicroSparks(shield.parent.DrawPos, shield.parent.Map);
                    FleckMakerCE.ThrowLightningGlow(newExactPos, projectile.Map, 0.5f);
                }
                // Actually Intercept the projectile
                projectile.InterceptProjectile(shield, newExactPos, true);
            }
        }

        // Adapted from VanillaExpandedFramework patch
        private IEnumerable<IEnumerable<IntVec3>> ShieldZonesCallback(Thing pawnToSuppress)
        {
            refreshShields(pawnToSuppress.Map);
            List<IEnumerable<IntVec3>> result = new List<IEnumerable<IntVec3>>();
            if (!shields.Any())
            {
                return result;
            }
            foreach (var shield in shields)
            {
                if (shield.shutDown)
                {
                    continue;
                }
                result.Add(GenRadial.RadialCellsAround(shield.parent.Position, shield.radius, true));
            }
            return result;
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
