using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using Harmony;

namespace CombatExtended.Harmony
{
    static class Harmony_HediffWithComps_BleedRate_Patch
    {
        public static void Postfix(HediffWithComps __instance, ref float __result)
        {
            if (__result > 0)
            {
                // Check for stabilized comp
                HediffComp_Stabilize comp = __instance.TryGetComp<HediffComp_Stabilize>();
                if (comp != null)
                {
                    __result = __result * (comp.BleedModifier);
                }
            }
        }
    }
}
