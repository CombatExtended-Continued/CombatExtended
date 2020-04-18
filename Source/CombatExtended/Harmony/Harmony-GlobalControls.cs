using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(GlobalControlsUtility), nameof(GlobalControlsUtility.DoDate))]
    internal static class Harmony_GlobalControls
    {
        private static void Postfix(ref float curBaseY)
        {
            Find.CurrentMap.GetComponent<WeatherTracker>().DoWindGUI(UI.screenWidth - 200f + 8f, ref curBaseY);
        }
    }
}