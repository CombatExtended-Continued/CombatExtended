using System;
using System.Collections.Generic;
using System.Linq;
using CombatExtended.AI;
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
        private static Verb verb;
        private static Pawn pawn;
        private static Map map;        
        private static IntVec3 target;
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
                verb = newReq.verb;
                pawn = newReq.caster;
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
                for (int i = 0; i < interceptors.Count; i++)
                {
                    CompProjectileInterceptor interceptor = interceptors[i];
                    if (interceptor.Active && interceptor.parent.Position.DistanceTo(c) < interceptor.Props.radius)
                    {
                        if (interceptor.parent.Position.PawnsInRange(map, interceptor.Props.radius).All(p => p.HostileTo(pawn)))
                            __result -= 15.0f;
                        else
                            __result += 5f;
                    }
                }
                float visibilityCost = 0;
                if (sightGrid != null)
                    visibilityCost += sightGrid.GetCellSightCoverRating(c) * 1.5f;
                if (turretTracker != null && turretTracker.GetVisibleToTurret(c))
                    visibilityCost += 5f;

                if (visibilityCost > 0)
                {
                    __result -= visibilityCost;
                    __result -= dangerTracker.DangerAt(c) * 5;
                    if (lightingTracker.IsNight)
                        __result -= lightingTracker.CombatGlowAt(c) * 6;
                }                
                //if (verb != null && verb.EffectiveRange > 0)
                //    __result *= Mathf.Clamp(1f - Mathf.Abs(c.DistanceTo(target) - verb.EffectiveRange * 0.75f) / verb.EffectiveRange * 0.75f, 0.75f, 1.35f);
            }
        }
    }
}
