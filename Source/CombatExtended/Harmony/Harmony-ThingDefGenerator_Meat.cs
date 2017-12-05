using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using Harmony;
using System.Reflection;

namespace CombatExtended.Harmony
{
    [HarmonyPatch]
    public static class Harmony_ThingDefGenerator_Meat
    {
        static MethodBase TargetMethod()
        {
            var type = AccessTools.Inner(typeof(ThingDefGenerator_Meat), "<ImpliedMeatDefs>c__Iterator0");
            return AccessTools.Method(type, "MoveNext");
        }
        
        public static void Postfix(IEnumerator<ThingDef> __instance, bool __result)
        {
            if (__result)
            {
                __instance.Current.SetStatBaseValue(CE_StatDefOf.Bulk, 0.2f);
            }
        }
    }
}
