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
        internal static void Postfix(Hediff __instance, ref float __result)
        {
            HediffComp_Stabilize comp = (__instance as HediffWithComps)?.TryGetComp<HediffComp_Stabilize>() ?? null;
            if (comp != null)
            {
                __result = Mathf.Max(__result, __instance.BleedRate * 1.5f + comp.StabilizedBleed * 0.3f);
            }
        }

    }
}
