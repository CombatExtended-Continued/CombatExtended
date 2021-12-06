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
        private static SightTracker.SightReader sightReader;
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
                    newReq.caster.GetSightReader(out sightReader);
            }

            public static void Postfix()
            {
                pawn = null;
                verb = null;
                map = null;
                sightReader = null;
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
                float sightCost = 0;
                if (sightReader != null)
                    sightCost += 6 - Mathf.Min(sightReader.GetSightCoverRating(c), 6);                

                if (sightCost > 0)
                {
                    __result += sightCost;
                    __result += 2 - dangerTracker.DangerAt(c);
                    if (lightingTracker.IsNight)
                        __result *= 1 - lightingTracker.CombatGlowAt(c) / 2f;
                }
                if (range > 0)
                {
                    float rangeSqr = range * range;
                    //
                    //__result += (warmupTime - c.DistanceToSquared(target) / rangeSqr) * 4f;
                    __result -= c.PawnsInRange(map, 8).Count(c => c.Faction == pawn.Faction) * 2.0f;
                    __result -= Mathf.Abs(rangeSqr * 0.75f - c.DistanceToSquared(target)) / (rangeSqr * 0.75f) * 8;
                }                
            }
        }
    }
}
