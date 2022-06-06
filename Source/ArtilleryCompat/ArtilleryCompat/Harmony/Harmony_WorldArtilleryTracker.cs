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
using CombatExtended.Utilities;
using RimWorld.Planet;

namespace CombatExtended.Compatibility.Artillery
{
    // TODO: Remove this dirty hack to work around VFE bug
    // [HarmonyPatch(typeof(WorldArtilleryTracker), nameof(WorldArtilleryTracker.ExposeData))] -- commented out because we need to delay running this
    public class Harmony_WorldArtilleryTracker_ExposeData {
	private static HashSet<ArtilleryComp> cached_cachedArtilleryCompsBombarding = null;
	public static bool Prefix(WorldArtilleryTracker __instance) {
	    if (Scribe.mode == LoadSaveMode.Saving) {
		if (__instance.cachedArtilleryCompsBombarding.Any()) {
		    cached_cachedArtilleryCompsBombarding = __instance.cachedArtilleryCompsBombarding;
		    __instance.cachedArtilleryCompsBombarding = new HashSet<ArtilleryComp>();
		}
		else {
		    cached_cachedArtilleryCompsBombarding = null;
		}
	    }
	    return true;
	}
	public static void Postfix(WorldArtilleryTracker __instance) {
	    if (Scribe.mode == LoadSaveMode.Saving) {
		if (cached_cachedArtilleryCompsBombarding != null) {
		    __instance.cachedArtilleryCompsBombarding = cached_cachedArtilleryCompsBombarding;
		    cached_cachedArtilleryCompsBombarding = null;
		}
	    }
	}
    }
}
