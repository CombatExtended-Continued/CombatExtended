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
    [HarmonyPatch(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve")]
    static class Harmony_DefGenerator
    {
        public static void Postfix()
        {
            // Resolve implied crit defs
            var enumerable = CritDefGenerator.ImpliedCritDefs().ToArray();
            foreach (DamageDef current in enumerable)
            {
                current.PostLoad();
                DefDatabase<DamageDef>.Add(current);
            }
        }
    }
}
