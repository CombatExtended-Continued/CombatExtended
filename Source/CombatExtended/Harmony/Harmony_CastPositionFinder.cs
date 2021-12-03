using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CombatExtended.AI;
using CombatExtended.HarmonyCE.Compatibility;
using CombatExtended.Utilities;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended.HarmonyCE
{
    public static class Harmony_CastPositionFinder
    {
        private static float range;        
        private static Verb verb;
        private static Pawn pawn;
        private static Map map;        
        private static IntVec3 target;
        private static float warmupTime;
        private static SightGrid sightGrid;
        private static TurretTracker turretTracker;
        private static DangerTracker dangerTracker;
        private static LightingTracker lightingTracker;
        private static List<CompProjectileInterceptor> interceptors;

        [HarmonyPatch(typeof(CastPositionFinder), nameof(CastPositionFinder.TryFindCastPosition))]
        public static class CastPositionFinder_TryFindCastPosition_Patch
        {
            public static void Prefix(CastPositionRequest newReq)
            {
                //newReq.ma
                verb = newReq.verb;
                range = verb.EffectiveRange;                
                pawn = newReq.caster;
                warmupTime = verb?.verbProps.warmupTime ?? 1;
                map = newReq.caster?.Map;
                target = newReq.target.Position;
                interceptors = map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor)
                                               .Select(t => t.TryGetComp<CompProjectileInterceptor>())
                                               .ToList();

                dangerTracker = map.GetDangerTracker();
                lightingTracker = map.GetLightingTracker();
                if(map.ParentFaction != newReq.caster?.Faction)
                    turretTracker = map.GetComponent<TurretTracker>();
                if (newReq.caster != null && newReq.caster.Faction != null)
                {
                    if (!map.GetComponent<SightTracker>().TryGetGrid(newReq.caster, out sightGrid))                        
                        sightGrid = null;                        
                }                
            }

            public static void Postfix()
            {
                pawn = null;
                verb = null;
                map = null;
                sightGrid = null;
                dangerTracker = null;
                lightingTracker = null;
                turretTracker = null;
            }
        }
        
        [HarmonyPatch(typeof(CastPositionFinder), nameof(CastPositionFinder.CastPositionPreference))]
        public static class CastPositionFinder_CastPositionPreference_Patch
        {         
            public static void Postfix(IntVec3 c, ref float __result)
            {
                if (__result == -1)
                    return;

                for (int i = 0; i < interceptors.Count; i++)
                {
                    CompProjectileInterceptor interceptor = interceptors[i];
                    if (interceptor.Active && interceptor.parent.Position.DistanceToSquared(c) < interceptor.Props.radius * interceptor.Props.radius)
                    {
                        if (interceptor.parent.Position.PawnsInRange(map, interceptor.Props.radius).All(p => p.HostileTo(pawn)))
                            __result -= 15.0f;
                        else
                            __result += 8;
                    }
                }
                float visibilityCost = 0;
                if (sightGrid != null)
                    visibilityCost += sightGrid.GetCellSightCoverRating(c);
                if (turretTracker != null && turretTracker.GetVisibleToTurret(c))
                    visibilityCost += 2;

                if (visibilityCost > 0)
                {
                    __result -= visibilityCost;
                    __result *= 2f- dangerTracker.DangerAt(c) / 2;
                    if (lightingTracker.IsNight)
                        __result *= 1 - lightingTracker.CombatGlowAt(c) / 2f;
                }
                if (range > 0)
                {
                    float rangeSqr = c.DistanceToSquared(target);
                    __result -= (warmupTime - c.DistanceToSquared(target) / rangeSqr) * 4f;
                    __result *= Mathf.Clamp(1f - Mathf.Abs(c.DistanceToSquared(target) - rangeSqr * 0.75f) / (rangeSqr * 0.75f), 0.75f, 1.35f);
                }                
            }
        }
    }
}
