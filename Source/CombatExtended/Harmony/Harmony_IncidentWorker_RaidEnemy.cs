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
    public static class Harmony_IncidentWorker_RaidEnemy
    {
        [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), nameof(IncidentWorker_RaidEnemy.ResolveRaidPoints))]
        public static class Harmony_IncidentWorker_RaidEnemy_ResolveRaidPoints
        {
            public static void Postfix(ref IncidentParms parms)
            {
                FactionStrengthTracker tracker = parms.faction.GetStrengthTracker();
                if (tracker != null)
                {
                    parms.points *= tracker.RaidPointsMultiplier;
                }
            }
        }

        [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), nameof(IncidentWorker_RaidEnemy.TryExecuteWorker))]
        public static class Harmony_IncidentWorker_RaidEnemy_TryExecuteWorker
        {
            public static bool Prefix(IncidentParms parms)
            {
                FactionStrengthTracker tracker = parms.faction.GetStrengthTracker();
                if (tracker != null)
                {
                    return tracker.CanRaid;
                }
                return true;
            }
        }
    }
}

