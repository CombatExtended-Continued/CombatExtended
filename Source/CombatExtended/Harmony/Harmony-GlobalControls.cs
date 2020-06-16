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
        private const float magicExtraOffset = 8f;

        private static void Postfix(ref float curBaseY)
        {
            float offsetXFromOriginalMethod = UI.screenWidth - 200f;
            Find.CurrentMap?.GetComponent<WeatherTracker>().DoWindGUI(offsetXFromOriginalMethod + magicExtraOffset, ref curBaseY);
        }
    }
}