using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Harmony;
using Verse;
using RimWorld;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CombatExtended.Harmony
{
    [HarmonyPatch(typeof(MassUtility), "Capacity")]
    static class Harmony_MassUtility_Capacity
    {
        static void Postfix(float __result, Pawn p)
        {
            if (__result != 0)
                __result = p.GetStatValue(CE_StatDefOf.CarryWeight);
        }
    }
}