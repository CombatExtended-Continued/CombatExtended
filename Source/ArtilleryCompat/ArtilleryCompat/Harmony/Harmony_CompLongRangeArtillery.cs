using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;
using VFESecurity;
using CombatExtended;
using RimWorld;
using Verse.Sound;


namespace CombatExtended.Compatibility
{
    [HarmonyPatch(typeof(CompLongRangeArtillery), "ResetWarmupTicks")]
    public class Harmony_CompLongRangeArtillery_ResetWarmupTicks {
        public static bool Prefix(CompLongRangeArtillery __instance) {
            if (__instance.parent is Building_TurretGunCE btgce) {
                __instance.warmupTicksLeft = Mathf.Max(1, btgce.def.building.turretBurstWarmupTime.SecondsToTicks());;
                return false;
            }
            return true;
        }

    }
}
