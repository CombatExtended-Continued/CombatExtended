using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using RimWorld;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CombatExtended.HarmonyCE
{
    public static class Harmony_MapPawns
    {
        [HarmonyPatch(typeof(MapPawns), nameof(MapPawns.RegisterPawn))]
        public static class Harmony_MapPawns_RegisterPawn
        {
            public static void Prefix(MapPawns __instance, Pawn p) => __instance.map.GetComponent<SightTracker>().Register(p);
        }

        [HarmonyPatch(typeof(MapPawns), nameof(MapPawns.DeRegisterPawn))]
        public static class Harmony_MapPawns_DeRegisterPawn
        {
            public static void Prefix(MapPawns __instance, Pawn p) => __instance.map.GetComponent<SightTracker>().DeRegister(p);
        }
    }
}

