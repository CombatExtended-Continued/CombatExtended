using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CombatExtended.Harmony
{
    [HarmonyPatch(typeof(Region), "DangerFor")]
    internal static class Harmony_Region_DangerFor
    {
        internal static void Postfix(Pawn p, Region __instance, ref Danger __result)
        {
            if (p.GetStatValue(CE_StatDefOf.SmokeSensitivity) > 0)
            {
                int smokeCount = __instance?.ListerThings?.AllThings?.Where(t => t is Smoke)?.Count() ?? 0;
                if (smokeCount > 0)
                {
                    __result = Danger.Deadly;
                }
            }
        }
    }
}
