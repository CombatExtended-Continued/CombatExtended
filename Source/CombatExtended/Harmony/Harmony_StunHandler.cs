using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        /* STUN_TIME_DAMAGE_RATIO : How long a stun is based on the damage applied
         * ADAPTATION_TIME_RATIO : How long adaptation lasts, based on stun time
         * FLAT_ADAPTATION_TIME_REDUCTION_RATIO : The adaptation reduction based on damage, scaled to the adaptation time based on damage. 
         *
         * Because the adaptation time ratio only has an effect on the speed that adaptation decays over time, the only affect changing it has is on how much less damage is required
         * to cause an enemy to be stunned again over a certain period of time.
         * The amount of hits to restun an enemy is (1/FLAT_ADAPTATION_TIME_REDUCTION_RATIO)
         * The amount of DPS needed to re-stun an enemy before the stun wears off is damageperhit*(1/FLAT_ADAPTATION_TIME_REDUCTION_RATIO)
         * 1/(damageperhit*(1/FLAT_ADAPTATION_TIME_REDUCTION_RATIO)) / DPS) is the ratio of time they spend stunned 
         * The amount of damage to re-stun an entity instantly after it's stun wears off is ADAPTATION_TIME_RATIO / FLAT_ADAPTATION_TIME_REDUCTION_RATIO / STUN_TIME_DAMAGE_RATIO
         * when not factoring in adaptation decay over time.
         *
         * NOTE : Something possible to do if high DPS low DPH weapons are too strong is to apply a penalty if the hit isn't significantly higher than the adaptation time
         */

        // 
        private const float STUN_TIME_DAMAGE_RATIO = 15f; // The scaling factor of stuns based on damage
        private const float ADAPTATION_TIME_RATIO = STUN_TIME_DAMAGE_RATIO * 3; // Adaptation time based on stun time
        private const float FLAT_ADAPTATION_TIME_REDUCTION_RATIO = ADAPTATION_TIME_RATIO / 8; // Scaling of adaptation reduction
        private const float REDUCTION_MULTIPLIER_WHILE_STUNNED = 0.8f;
        private const float BUILDING_DEFAULT_BODYSIZE = 2f;

        public static bool Prefix(StunHandler __instance, DamageInfo dinfo, bool affectedByEMP, int ___EMPAdaptedTicksLeft, int ___stunTicksLeft, bool ___stunFromEMP)
        {
            float bodysize = BUILDING_DEFAULT_BODYSIZE;
            if (__instance.parent is Pawn)
            {
                Pawn p = (Pawn)__instance.parent;
                if (p.Downed || p.Dead) { return false; }
                bodysize = p.BodySize;
            }

            Type t = typeof(StunHandler);
            FieldInfo EMPAdaptedTicksLeft = t.GetField("EMPAdaptedTicksLeft", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            FieldInfo StunTicksLeft = t.GetField("stunTicksLeft", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            FieldInfo StunFromEMP = t.GetField("stunFromEMP", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            float multiplier = bodysize / Mathf.Pow(bodysize, 2); // Multiplier based on bodysize, smaller for larger creatures
            if (dinfo.Def == DamageDefOf.EMP && affectedByEMP)
            {
                if(___EMPAdaptedTicksLeft > 0)
                {
                    
                    if (___stunTicksLeft > 0) 
                    { EMPAdaptedTicksLeft.SetValue(__instance, ___EMPAdaptedTicksLeft - (int)(dinfo.Amount * FLAT_ADAPTATION_TIME_REDUCTION_RATIO * REDUCTION_MULTIPLIER_WHILE_STUNNED * multiplier)); }
                    else 
                    { EMPAdaptedTicksLeft.SetValue(__instance, ___EMPAdaptedTicksLeft - (int)(dinfo.Amount * FLAT_ADAPTATION_TIME_REDUCTION_RATIO * multiplier)); }
                    
                    
                    if(___EMPAdaptedTicksLeft < 0)
                    {
                        float stun_time = Mathf.Abs(___EMPAdaptedTicksLeft / (ADAPTATION_TIME_RATIO / STUN_TIME_DAMAGE_RATIO));
                        __instance.StunFor_NewTmp((int)(stun_time), dinfo.Instigator, true, true);
                        EMPAdaptedTicksLeft.SetValue(__instance, (int)(stun_time * ADAPTATION_TIME_RATIO));
                        StunFromEMP.SetValue(__instance, true);
                        return false;
                    }
                    MoteMaker.ThrowText(new Vector3((float)__instance.parent.Position.x + 1f, (float)__instance.parent.Position.y, (float)__instance.parent.Position.z + 1f), __instance.parent.Map, "Adapted".Translate(), Color.white, -1f);
                }
                else
                {
                    __instance.StunFor_NewTmp((int)(dinfo.Amount * STUN_TIME_DAMAGE_RATIO * multiplier), dinfo.Instigator, true, true);
                    EMPAdaptedTicksLeft.SetValue(__instance, (int)(dinfo.Amount * ADAPTATION_TIME_RATIO));
                    StunFromEMP.SetValue(__instance, true);
                }
            }
            return false; 
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

