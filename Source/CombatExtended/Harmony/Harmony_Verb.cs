using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Verse;
using RimWorld;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(Verb), "IsStillUsableBy")]
    internal static class Harmony_Verb
    {
        internal static void Postfix(Verb __instance, ref bool __result, Pawn pawn)
        {
            if (__result)
            {
                var tool = __instance.tool as ToolCE;
                if (tool != null)
                {
                    __result = tool.restrictedGender == Gender.None || tool.restrictedGender == pawn.gender;
                }
            }
        }
    }
}
