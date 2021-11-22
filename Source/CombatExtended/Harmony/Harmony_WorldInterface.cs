using System;
using RimWorld.Planet;
using HarmonyLib;
using Verse;
using RimWorld;
using CombatExtended.WorldObjects;

namespace CombatExtended.HarmonyCE
{
    public static class Harmony_WorldInterface
    {
        [HarmonyPatch(typeof(WorldInterface), nameof(WorldInterface.WorldInterfaceOnGUI))]
        public static class Harmony_WorldInterface_WorldInterfaceOnGUI
        {
            public static void Postfix()
            {
                try
                {
                    if (WorldRendererUtility.WorldRenderedNow && ExpandableWorldObjectsUtility.TransitionPct <= 0.25f)
                    {
                        WorldHealthGUIUtility.OnGUIWorldObjectHealth();
                    }
                }
                catch(Exception er)
                {
                    Log.Error($"CE: Harmony_WorldInterface {er}");
                }
            }
        }
    }
}

