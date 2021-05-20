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
        private const int FLAT_ADAPTATION_TIME_REDUCTION = 2;
        private const float ADAPTATION_TIME_MULTIPLIER = 0.75f;
        private const float LOGARITHM_BASE = 6f;

        public static bool Prefix(StunHandler __instance, DamageInfo dinfo, bool affectedByEMP, int ___EMPAdaptedTicksLeft, int ___stunTicksLeft, bool ___stunFromEMP)
        {
            // This is purely logic for things that are adapted to make it easier to re-stun them. It doesn't do anything if they're not stunned, and it doesn't directly stun them.
            // Whenever an EMP hit is taken the adaptation is reduced by a certain amount based on the damage of the hit, clamped to a certain value so it always takes more than 1 hit to restun.
            Pawn p = null;
            if (__instance.parent is Pawn)
            {
                p = (Pawn)__instance.parent;
                if (p.Downed || p.Dead) { return false; }
            }

            if (___EMPAdaptedTicksLeft > 0 && ___stunTicksLeft == 0 && dinfo.Def == DamageDefOf.EMP && affectedByEMP)
            {
                if (p != null)
                {
                    // TODO : Add the projectile damage into this calculation, making it reduce this multiplier by some factor based on the damage
                    float new_mult = Mathf.Clamp(ADAPTATION_TIME_MULTIPLIER + 0.25f * Mathf.Log(p.BodySize, LOGARITHM_BASE), 0, 1);
                    ___EMPAdaptedTicksLeft = (int)((float)___EMPAdaptedTicksLeft * new_mult);
                }
                else { ___EMPAdaptedTicksLeft = (int)((float)___EMPAdaptedTicksLeft * ADAPTATION_TIME_MULTIPLIER); }// Casting removes the decimal so it always rounds down for positive values
                if (___EMPAdaptedTicksLeft > FLAT_ADAPTATION_TIME_REDUCTION) { ___EMPAdaptedTicksLeft -= FLAT_ADAPTATION_TIME_REDUCTION; }
            }
            return true;
        }
	
        public static void Postfix(StunHandler __instance, DamageInfo dinfo, bool affectedByEMP)
        {
            if (dinfo.Def == DamageDefOf.EMP)
            {
                var dmgAmount = dinfo.Amount;
                if (!affectedByEMP) { dmgAmount = Mathf.RoundToInt(dmgAmount * 0.25f); }
                var newDinfo = new DamageInfo(CE_DamageDefOf.Electrical, dmgAmount, 9999, // Hack to avoid double-armor application (EMP damage reduced -> proportional electric damage reduced again)
                    dinfo.Angle, dinfo.Instigator, dinfo.HitPart, dinfo.Weapon, dinfo.Category);
                __instance.parent.TakeDamage(newDinfo);
            }
        }
    }
}

