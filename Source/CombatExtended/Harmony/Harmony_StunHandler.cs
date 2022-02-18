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
        public static bool Prefix(StunHandler __instance, DamageInfo dinfo, ref int ___EMPAdaptedTicksLeft, ref int ___stunTicksLeft, ref bool ___stunFromEMP)
        {

            Pawn pawn = __instance.parent as Pawn;
            float bodySize = 1.0f;
            if (pawn != null)
            {
                if (pawn.Downed || pawn.Dead)
                {
                    return false;
                }
                bodySize = pawn.BodySize;
            }

            if (dinfo.Def == DamageDefOf.EMP && __instance.parent is Pawn p && !(p.RaceProps?.IsFlesh ?? false))
            {
                if (___EMPAdaptedTicksLeft > 0)
                {
                    int newStunAdaptedTicks = Mathf.RoundToInt(dinfo.Amount * 45 * bodySize);
                    int newStunTicks = Mathf.RoundToInt(dinfo.Amount * 30);

                    float stunResistChance = ((float)___EMPAdaptedTicksLeft / (float)newStunAdaptedTicks) * 15;

                    if (Rand.Value > stunResistChance)
                    {
                        ___EMPAdaptedTicksLeft += Mathf.RoundToInt(dinfo.Amount * 45 * bodySize);

                        if (___stunTicksLeft > 0 && newStunTicks > ___stunTicksLeft)
                        {
                            ___stunTicksLeft = newStunTicks;
                        }
                        else
                        {
                            __instance.StunFor(newStunTicks, dinfo.Instigator, true, true);
                        }
                    }
                    else
                    {
                        MoteMakerCE.ThrowText(new Vector3((float)__instance.parent.Position.x + 1f, (float)__instance.parent.Position.y, (float)__instance.parent.Position.z + 1f), __instance.parent.Map, "Adapted".Translate(), Color.white, -1f);
                        int adaptationReduction = Mathf.RoundToInt(Mathf.Sqrt(dinfo.Amount * 45));

                        if (adaptationReduction < ___EMPAdaptedTicksLeft)
                        {
                            ___EMPAdaptedTicksLeft -= adaptationReduction;
                        }
                        else
                        {
                            float adaptationReductionRatio = (adaptationReduction - ___EMPAdaptedTicksLeft) / adaptationReduction;
                            newStunAdaptedTicks = Mathf.RoundToInt(newStunAdaptedTicks * adaptationReductionRatio);
                            newStunTicks = Mathf.RoundToInt(newStunTicks * adaptationReductionRatio);

                            if (___stunTicksLeft > 0 && newStunTicks > ___stunTicksLeft)
                            {
                                ___stunTicksLeft = newStunTicks;
                            }
                            else
                            {
                                __instance.StunFor(newStunTicks, dinfo.Instigator, true, true);
                            }
                        }
                    }

                }
                else
                {
                    __instance.StunFor(Mathf.RoundToInt(dinfo.Amount * 30f), dinfo.Instigator, true, true);
                    ___EMPAdaptedTicksLeft = Mathf.RoundToInt(dinfo.Amount * 45 * bodySize);
                    ___stunFromEMP = true;

                }
            }
            return true;
        }
        public static void Postfix(StunHandler __instance, DamageInfo dinfo)
        {
            if (dinfo.Def == DamageDefOf.EMP)
            {
                var dmgAmount = dinfo.Amount;
                if (__instance.parent is Pawn p && (p.RaceProps?.IsFlesh ?? false)) dmgAmount = Mathf.RoundToInt(dmgAmount * 0.25f);
                var newDinfo = new DamageInfo(CE_DamageDefOf.Electrical, dmgAmount, 9999, // Hack to avoid double-armor application (EMP damage reduced -> proportional electric damage reduced again)
                    dinfo.Angle, dinfo.Instigator, dinfo.HitPart, dinfo.Weapon, dinfo.Category);
                __instance.parent.TakeDamage(newDinfo);
            }
        }
    }
}
