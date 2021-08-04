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
                if (map != null)
                {
                    dangerTracker = map.GetDangerTracker();
                    lightingTracker = map.GetLightingTracker();
                }
                else
                {
                    dangerTracker = null;
                    lightingTracker = null;
                }
            }

            public static void Postfix()
            {
                pawn = null;
                verb = null;
                map = null;
                dangerTracker = null;
                lightingTracker = null;
            }
        }

        [HarmonyPatch(typeof(CastPositionFinder), nameof(CastPositionFinder.CastPositionPreference))]
        public static class CastPositionFinder_CastPositionPreference_Patch
        {
            public static void Postfix(IntVec3 c, ref float __result)
            {
                if (dangerTracker != null)
                {
                    __result -= dangerTracker.DangerAt(c) * 4;
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
                    if (lightingTracker.IsNight)
                        __result *= 1f - lightingTracker.CombatGlowAt(c) / 2f;
                    if (verb != null && verb.EffectiveRange > 0)
                        __result *= Mathf.Clamp(1f - Mathf.Abs(c.DistanceTo(target) - verb.EffectiveRange * 0.75f) / verb.EffectiveRange * 0.75f, 0.75f, 1.35f);
                }
            }
        }
    }
}
