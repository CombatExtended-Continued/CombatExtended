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


namespace CombatExtended.Compatibility.Artillery
{
    [HarmonyPatch(typeof(ActiveArtilleryStrike), "Speed", MethodType.Getter)]
    public class Harmony_ActiveArtilleryStrike_Speed {
        public static bool Prefix(ActiveArtilleryStrike __instance, out float __result) {
            var shellDef = __instance.shellDef;
            if (shellDef is AmmoDef ammoDef) {
                __result = ammoDef.GetProjectileProperties().speed;
		if (__result == 0) {
		    __result = __instance.missRadius;
		}
                if (__result == 0) {
                    __result = 170f;
                }
                return false;
            }
            __result = 0;
            return true;
        }
    }
}
