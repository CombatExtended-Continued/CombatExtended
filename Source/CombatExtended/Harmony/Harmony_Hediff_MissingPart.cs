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
    // FIXME: Destructive detour
    [HarmonyPatch(typeof(Hediff_MissingPart))]
    [HarmonyPatch("IsFreshNonSolidExtremity", MethodType.Getter)]
    static class Harmony_Hediff_MissingPart_IsFresh_Patch
    {
        public static bool Prefix(Hediff_MissingPart __instance, ref bool __result)
        {
            __result = Current.ProgramState != ProgramState.Entry
                && __instance.IsFresh
                && !__instance.Part.def.IsSolid(__instance.Part, __instance.pawn.health.hediffSet.hediffs) 
                && !__instance.ParentIsMissing
                && __instance.lastInjury != HediffDefOf.SurgicalCut;
            return false;
        }
    }
}
