using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Verse;
using RimWorld;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(PawnCapacityUtility), "CalculateNaturalPartsAverageEfficiency")]
    internal static class Harmony_PawnCapacityUtility
    {
        internal static bool Prefix(ref float __result, HediffSet diffSet, BodyPartGroupDef bodyPartGroup)
        {
            var primary = diffSet.pawn.equipment?.Primary;
            if (primary?.def.tools != null && primary.def.tools.Any(t => t.linkedBodyPartsGroup == bodyPartGroup))
            {
                __result = 1;
                return false;
            }
            return true;
        }
    }
}
