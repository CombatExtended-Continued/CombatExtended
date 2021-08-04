using System;
using HarmonyLib;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended.HarmonyCE
{
    public static class Harmony_CastPositionFinder
    {
        private static Map map;
        private static DangerTracker dangerTracker;
        private static LightingTracker lightingTracker;

        [HarmonyPatch(typeof(CastPositionFinder), nameof(CastPositionFinder.TryFindCastPosition))]
        public static class CastPositionFinder_TryFindCastPosition_Patch
        {
            public static void Prefix(CastPositionRequest newReq)
            {
                map = newReq.caster?.Map;
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
        }

        [HarmonyPatch(typeof(CastPositionFinder), nameof(CastPositionFinder.CastPositionPreference))]
        public static class CastPositionFinder_CastPositionPreference_Patch
        {
            public static void Postfix(IntVec3 c, ref float __result)
            {
                if (dangerTracker != null)
                {
                    __result -= dangerTracker.DangerAt(c) * 4;
                    __result *= 1f - lightingTracker.CombatGlowAt(c) / 2f;
                }
            }
        }
    }
}
