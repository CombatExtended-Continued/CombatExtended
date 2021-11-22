using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using CombatExtended;
using RimWorld;

namespace CombatExtended.HarmonyCE
{
    public static class Harmony_Faction
    {
        [HarmonyPatch(typeof(Faction), nameof(Faction.Notify_LeaderDied))]
        public static class Harmony_Faction_Notify_LeaderDied
        {
            public static void Prefix(Faction __instance)
            {
                FactionStrengthTracker tracker = __instance.GetStrengthTracker();
                if(tracker != null)
                {
                    tracker.Notify_LeaderKilled();
                }
            }
        }
    }
}

