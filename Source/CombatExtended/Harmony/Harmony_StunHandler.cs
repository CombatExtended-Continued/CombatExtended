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
    [HarmonyPatch(typeof(StunHandler), "Notify_DamageApplied")]
    public static class Harmony_StunHandler_Notify_DamageApplied
    {
	public static bool Prefix(StunHandler __instance,
				  DamageInfo dinfo,
				  bool affectedByEMP,
				  int ___EMPAdaptedTicksLeft,
				  int ___stunTicksLeft,
				  bool ___stunFromEMP
				  )
	{

	    Pawn pawn = __instance.parent as Pawn;
	    if (pawn == null || pawn.Downed || pawn.Dead)
	    {
		return false;
	    }
	    float bodySize = pawn.BodySize;
	    
	    if (dinfo.Def == DamageDefOf.EMP && affectedByEMP)
	    {
		if (___EMPAdaptedTicksLeft > 0)
		{
		    int newStunAdaptedTicks = Mathf.RoundToInt(dinfo.Amount * 45 * bodySize);
		    int newStunTicks = Mathf.RoundToInt(dinfo.Amount * 30);

		    float stunResistChance = ((float) ___EMPAdaptedTicksLeft / (float) newStunAdaptedTicks) * 15;
		    void reStun()
		    {
			if (___stunTicksLeft > 0 && newStunTicks > ___stunTicksLeft)
			{
			    ___stunTicksLeft = newStunTicks;
			}
			else
			{
			    __instance.StunFor_NewTmp(newStunTicks, dinfo.Instigator, true, true);
			}
			
		    }
		    
		    if (UnityEngine.Random.value > stunResistChance)
		    {
			___EMPAdaptedTicksLeft += Mathf.RoundToInt(dinfo.Amount * 45 * bodySize);
			reStun();
		    }
		    else
		    {
			MoteMaker.ThrowText(new Vector3((float)__instance.parent.Position.x + 1f, (float)__instance.parent.Position.y, (float)__instance.parent.Position.z + 1f), __instance.parent.Map, "Adapted".Translate(), Color.white, -1f);
			int adaptationReduction = Mathf.RoundToInt(Mathf.Sqrt(dinfo.Amount * 45));
			
			if (adaptationReduction < ___EMPAdaptedTicksLeft) {
			    ___EMPAdaptedTicksLeft -= adaptationReduction;
			}
			else
			{
			    float adaptationReductionRatio = (adaptationReduction - ___EMPAdaptedTicksLeft) / adaptationReduction;
			    newStunAdaptedTicks = Mathf.RoundToInt(newStunAdaptedTicks * adaptationReductionRatio);
			    newStunTicks = Mathf.RoundToInt(newStunTicks * adaptationReductionRatio);
			    reStun();
			}
		    }
		    		    
		}
		else
		{
		    __instance.StunFor_NewTmp(Mathf.RoundToInt(dinfo.Amount * 30f), dinfo.Instigator, true, true);
		    ___EMPAdaptedTicksLeft = Mathf.RoundToInt(dinfo.Amount * 45 * bodySize);
		    ___stunFromEMP = true;
		    
		}
	    }
	    return true;
	}
        public static void Postfix(StunHandler __instance, DamageInfo dinfo, bool affectedByEMP)
        {
            if (dinfo.Def == DamageDefOf.EMP)
            {
                var dmgAmount = dinfo.Amount;
                if (!affectedByEMP) dmgAmount = Mathf.RoundToInt(dmgAmount * 0.25f);
                var newDinfo = new DamageInfo(CE_DamageDefOf.Electrical, dmgAmount, 9999, // Hack to avoid double-armor application (EMP damage reduced -> proportional electric damage reduced again)
                    dinfo.Angle, dinfo.Instigator, dinfo.HitPart, dinfo.Weapon, dinfo.Category);
                __instance.parent.TakeDamage(newDinfo);
            }
        }
    }
}
