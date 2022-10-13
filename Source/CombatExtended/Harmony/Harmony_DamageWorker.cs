using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using RimWorld;
using UnityEngine;
using HarmonyLib;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(DamageWorker), nameof(DamageWorker.Apply))]
    internal static class Harmony_DamageWorker_Apply
    {
	private static ThingDef smallFragment = null;
	private static ThingDef largeFragment = null;
	private static DamageDef demolish = null;
	public static bool Prefix(DamageWorker __instance, DamageInfo dinfo, Thing victim)
	{
	    if (!Controller.settings.FragmentsFromWalls)
	    {
		return true;
	    }
	    if (victim.def.useHitPoints && dinfo.Def.harmsHealth && dinfo.Def != DamageDefOf.Mining)
	    {
		if (victim.def.category == ThingCategory.Building)
		{
		    if (smallFragment == null)
		    {
			smallFragment = DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.defName == "Fragment_Small").First();
			largeFragment = DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.defName == "Fragment_Large").First();
			demolish = DefDatabase<DamageDef>.AllDefsListForReading.Where(d => d.defName == "Demolish").First();
		    }
		    if (dinfo.Def == demolish) {
			return true;
		    }
		    bool isSharp = dinfo.Def.armorCategory == DamageArmorCategoryDefOf.Sharp;
		    Vector3 pos;
		    pos = victim.Position.ToVector3Shifted();
		    float num = dinfo.Amount;

		    num *= dinfo.Def.buildingDamageFactor;
		    if (victim.def.passability == Traversability.Impassable)
		    {
			num *= dinfo.Def.buildingDamageFactorImpassable;
		    }
		    else
		    {
			num *= dinfo.Def.buildingDamageFactorPassable;
		    }
		    float hitPoints = victim.HitPoints;
		    float maxHitPoints = victim.MaxHitPoints;
		    bool max = false;
		    if (num > hitPoints)
		    {
			max = true;
		    }
		    int fragmentDamage = (int) (Mathf.Max(num/10f, Mathf.Clamp01(num / hitPoints) * num));
		    if (isSharp)
		    {
			fragmentDamage /= 2;
		    }

		    int largeFragments = fragmentDamage / 37;
		    int smallFragments = (fragmentDamage % 37) / 9;
		    
		    smallFragments += 4 * ((largeFragments / 2) + largeFragments % 2);
		    largeFragments /= 2;

		    var frontArc = new FloatRange(dinfo.Angle + 90, dinfo.Angle + 270); 
		    var backArc = new FloatRange(dinfo.Angle - 60, dinfo.Angle + 60);

		    var map = victim.Map;
		    var height = new FloatRange(0, new CollisionVertical(victim).Max).RandomInRange;
		    if (max)
		    {
			backArc = new FloatRange(dinfo.Angle - 90, dinfo.Angle + 90);
		    }

		    if (smallFragments > 0)
		    {
			int reflectedFrags = (int) (smallFragments * (hitPoints / maxHitPoints));
			smallFragments -= reflectedFrags;
			if (reflectedFrags > 0) {
			    var fr = CompFragments.FragRoutine(pos,
							       map,
							       height,
							       victim,
							       new ThingDefCountClass(smallFragment, smallFragments),
							       1,
							       0.2f,
							       new FloatRange(0.5f, 5),
							       frontArc,
							       1f,
							       false);
			    while (fr.MoveNext()) { }
			}
			{
			    var fr = CompFragments.FragRoutine(pos,
							       map,
							       height,
							       victim,
							       new ThingDefCountClass(smallFragment, smallFragments),
							       1,
							       0.2f,
							       new FloatRange(0.5f, 5),
							       backArc,
							       1f,
							       false);
			    while (fr.MoveNext()) { }
			}

		    }
		    if (largeFragments > 0)
		    {
			var fr = CompFragments.FragRoutine(pos,
							   map,
							   height,
							   victim,
							   new ThingDefCountClass(largeFragment, largeFragments),
							   1,
							   0.2f,
							   new FloatRange(0.5f, 5),
							   backArc,
							   1f,
							   false);
			
			while (fr.MoveNext()) {}
			
		    }		    
		}
	    }
	    return true;
	}
    }
}
