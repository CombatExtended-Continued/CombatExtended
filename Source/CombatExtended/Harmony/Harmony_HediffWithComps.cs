using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace CombatExtended.HarmonyCE
{
    static class Harmony_HediffWithComps_BleedRate_Patch
    {
        public static void Postfix(Hediff __instance, ref float __result)
        {
            if (__result > 0)
            {
                // Check for stabilized comp
                HediffComp_Stabilize comp = (__instance as HediffWithComps)?.TryGetComp<HediffComp_Stabilize>() ?? null;
                if (comp != null)
                {
                    __result = __result * (comp.BleedModifier);
                }
            }
        }
    }
}
