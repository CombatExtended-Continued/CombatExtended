using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(Hediff), "TendPriority", MethodType.Getter)]
    class Harmony_HediffTendPriority
    {
        internal static bool Prefix(Hediff __instance, ref float __result)
        {
            //copied from Verse
            float severityHeuristic = 0f;
            HediffStage curStage = __instance.CurStage;
            if (curStage != null && curStage.lifeThreatening)
            {
                severityHeuristic = Mathf.Max(severityHeuristic, 1f);
            }

            //If injury is stabilised, increase priority
            HediffComp_Stabilize comp = (__instance as HediffWithComps)?.TryGetComp<HediffComp_Stabilize>() ?? null;
            if (comp != null)
            {
                severityHeuristic = Mathf.Max(severityHeuristic, __instance.BleedRate * 1.5f + comp.StabilizedBleed * 0.3f);
            }
            else
            {
                severityHeuristic = Mathf.Max(severityHeuristic, __instance.BleedRate * 1.5f);
            }
            HediffComp_TendDuration hediffComp_TendDuration = __instance.TryGetComp<HediffComp_TendDuration>();
            if (hediffComp_TendDuration != null && hediffComp_TendDuration.TProps.severityPerDayTended < 0f)
            {
                severityHeuristic = Mathf.Max(severityHeuristic, 0.025f);
            }
            __result = severityHeuristic;
            return false;
        }

    }
}
