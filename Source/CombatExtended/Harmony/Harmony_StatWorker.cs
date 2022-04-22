using HarmonyLib;
using Verse;
using RimWorld;

namespace CombatExtended.HarmonyCE
{

    [HarmonyPatch(typeof(StatWorker), nameof(StatWorker.ShouldShowFor))]
    internal static class StatWorker_ShouldShowFor
    {
	internal static bool Prefix(ref bool __result, StatWorker __instance)
	{
	    if (!Controller.settings.ShowExtraStats) {
		if (__instance.stat == global::CombatExtended.CE_StatDefOf.ToughnessRating)
		{
		    __result = false;
		    return false;
		}
	    }
	    return true;
	}
    }

    [HarmonyPatch(typeof(StatWorker), nameof(StatWorker.GetStatDrawEntryLabel))]
    internal static class StatWorker_PatchValueToString
    {
        internal static bool Prefix(ref string __result, StatWorker __instance, StatDef stat, float value, ToStringNumberSense numberSense, StatRequest optionalReq, bool finalized = true)
        {
	    if (__instance.stat == global::RimWorld.StatDefOf.ArmorRating_Blunt ||
		    __instance.stat == global::RimWorld.StatDefOf.ArmorRating_Sharp) {
		if(optionalReq != null) {
		    if (optionalReq.Thing is Apparel apparel) {
			float minArmor = value;
			float maxArmor = value;
			if (apparel.def.HasModExtension<PartialArmorExt>()) {
			    foreach (ApparelPartialStat p in apparel.def.GetModExtension<PartialArmorExt>().stats) {
				
				float thisArmor = value;
				if (p.useStatic) {
				    value = p.staticValue;
				}
				else {
				    value *= p.mult;
				}
				if (thisArmor < minArmor) {
				    minArmor = thisArmor;
				}
				else if (thisArmor > maxArmor) {
				    maxArmor = thisArmor;
				}
			    }
			    __result = string.Format(stat.formatString, $"{minArmor} ~ {maxArmor}");
			    return false;
			}
		    }
		    else if (optionalReq.Def!=null && optionalReq.Def.HasModExtension<PartialArmorExt>()) {
			float minArmor = value;
			float maxArmor = value;
			var ext = optionalReq.Def.GetModExtension<PartialArmorExt>();
			foreach (ApparelPartialStat partstat in ext.stats)
			{
			    float thisArmor = value;
			    if (partstat.stat == stat)
			    {
				if (!partstat.useStatic)
				{
				    thisArmor *= partstat.mult;
				}
				else
				{
				    thisArmor = partstat.staticValue;
				}
			    }
			    if (thisArmor < minArmor) {
				minArmor = thisArmor;
			    }
			    else if (thisArmor > maxArmor) {
				maxArmor = thisArmor;
			    }
			    __result = string.Format(stat.formatString, $"{minArmor} ~ {maxArmor}");
			    return false;
			}
		    }
		}
	    }
	    return true;
        }
    }
}
