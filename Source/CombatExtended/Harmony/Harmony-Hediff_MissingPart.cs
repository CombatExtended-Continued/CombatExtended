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
    // FIXME: Destructive detour
    [HarmonyPatch(typeof(Hediff_MissingPart))]
    [HarmonyPatch("IsFreshNonSolidExtremity", MethodType.Getter)]
    static class Harmony_Hediff_MissingPart_IsFresh_Patch
    {
        public static bool Prefix(Hediff_MissingPart __instance, ref bool __result)
        {
            var hediff = Traverse.Create(__instance);
            __result = Current.ProgramState != ProgramState.Entry
                && __instance.IsFresh
                && !__instance.Part.def.IsSolid(__instance.Part, __instance.pawn.health.hediffSet.hediffs) 
                && !hediff.Property("ParentIsMissing").GetValue<bool>()
                && __instance.lastInjury != HediffDefOf.SurgicalCut;
            return false;
        }
    }
}
