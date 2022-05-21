using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Verse;
using CombatExtended.Loader;
using RimWorld;
using System.Collections.Generic;
using HarmonyLib;
using VFESecurity;

namespace CombatExtended.Compatibility.Artillery {
    [StaticConstructorOnStartup]
    public class ArtilleryCompat: IModPart {
	private static Harmony harmony;
        public Type GetSettingsType() {
            return null;
        }
        public IEnumerable<string> GetCompatList() {
            yield break;
        }
        public void PostLoad(ModContentPack content, ISettingsCE _) {
            harmony = new Harmony("CombatExtended.Artillery.HarmonyCE");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
	    LongEventHandler.QueueLongEvent(ArtilleryCompat.DelayedPatches, "CE_LongEvent_ArtilleryPatches", false, null);
        }
	public static void DelayedPatches() {
	    var target = AccessTools.Method(typeof(WorldArtilleryTracker), nameof(WorldArtilleryTracker.ExposeData));
	    var prefix = AccessTools.Method(typeof(Harmony_WorldArtilleryTracker_ExposeData), nameof(Harmony_WorldArtilleryTracker_ExposeData.Prefix));
	    var postfix = AccessTools.Method(typeof(Harmony_WorldArtilleryTracker_ExposeData), nameof(Harmony_WorldArtilleryTracker_ExposeData.Postfix));
	    harmony.Patch(target, new HarmonyMethod(prefix), new HarmonyMethod(postfix));

	    target = AccessTools.Method(typeof(CompLongRangeArtillery), nameof(CompLongRangeArtillery.ResetWarmupTicks));
	    prefix = AccessTools.Method(typeof(Harmony_CompLongRangeArtillery_ResetWarmupTicks), nameof(Harmony_CompLongRangeArtillery_ResetWarmupTicks.Prefix));
	    harmony.Patch(target, new HarmonyMethod(prefix), null);
	}
    }
}
