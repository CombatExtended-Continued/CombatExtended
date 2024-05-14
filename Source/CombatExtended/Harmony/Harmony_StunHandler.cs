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
    [HarmonyPatch(typeof(StunHandler), nameof(StunHandler.Notify_DamageApplied))]
    public static class Harmony_StunHandler_Notify_DamageApplied
    {
        public static bool Prefix(StunHandler __instance, DamageInfo dinfo, ref int ___stunTicksLeft, ref bool ___stunFromEMP, ref Dictionary<DamageDef, int> ___adaptationTicksLeft)
        {
            if (!__instance.CanBeStunnedByDamage(dinfo.Def))
            {
                return false;
            }

            Pawn pawn = __instance.parent as Pawn;
            float bodySize = 1.0f;
            float resistance = 1f;
            if (pawn != null)
            {
                if (pawn.Downed || pawn.Dead)
                {
                    return false;
                }
                bodySize = pawn.BodySize;
                if (!dinfo.Def.stunResistStat?.Worker.IsDisabledFor(pawn) ?? false)
                {
                    resistance = pawn.GetStatValue(dinfo.Def.stunResistStat);
                }
            }
            float stunTicks = dinfo.Def.constantStunDurationTicks ?? dinfo.Amount * 30f;
            // Makes the adapted tick and stun tick scales with bodysize
            // And add a chance of resisting new stun when adaptation ticks are not 0
            if (__instance.CanAdaptToDamage(dinfo.Def))
            {
                int newStunAdaptedTicks = Mathf.RoundToInt(dinfo.Amount * 45 * bodySize);
                int newStunTicks = Mathf.RoundToInt(stunTicks * Mathf.Clamp01(1f - resistance));
                // remember value is of value type, not reference type
                if (___adaptationTicksLeft.TryGetValue(dinfo.Def, out int adaptedTicksLeft) && adaptedTicksLeft > 0)
                {
                    float stunResistChance = ((float)adaptedTicksLeft / (float)newStunAdaptedTicks) * 15;

                    if (Rand.Value > stunResistChance)
                    {
                        ___adaptationTicksLeft[dinfo.Def] += newStunAdaptedTicks;

                        ___stunFromEMP = dinfo.Def == DamageDefOf.EMP;
                        // StunFor already uses Mathf.Max(stunTicksLeft, ticks) for ___stunTicksLeft
                        __instance.StunFor(newStunTicks, dinfo.Instigator, true, true);
                        return false;
                    }
                    else // Adapted
                    {
                        MoteMakerCE.ThrowText(new Vector3((float)__instance.parent.Position.x + 1f, (float)__instance.parent.Position.y, (float)__instance.parent.Position.z + 1f), __instance.parent.Map, "Adapted".Translate(), Color.white, -1f);
                        int adaptationReduction = Mathf.RoundToInt(Mathf.Sqrt(dinfo.Amount * 45));

                        if (adaptationReduction < adaptedTicksLeft)
                        {
                            ___adaptationTicksLeft[dinfo.Def] -= adaptationReduction;
                        }
                        else
                        {
                            float adaptationReductionRatio = (adaptationReduction - adaptedTicksLeft) / adaptationReduction;
                            ___adaptationTicksLeft[dinfo.Def] = Mathf.RoundToInt(newStunAdaptedTicks * adaptationReductionRatio);
                            newStunTicks = Mathf.RoundToInt(newStunTicks * adaptationReductionRatio);

                            // StunFor already uses Mathf.Max(stunTicksLeft, ticks) for ___stunTicksLeft
                            ___stunFromEMP = dinfo.Def == DamageDefOf.EMP;
                            __instance.StunFor(newStunTicks, dinfo.Instigator, true, true);
                        }
                        return false;
                    }

                }
                else // no adaptation ticks, full stun
                {
                    ___stunFromEMP = dinfo.Def == DamageDefOf.EMP;
                    __instance.StunFor(newStunTicks, dinfo.Instigator, true, true);
                    ___adaptationTicksLeft.SetOrAdd(dinfo.Def, newStunAdaptedTicks);
                    return false;
                }
            }
            return true;
        }
        public static void Postfix(StunHandler __instance, DamageInfo dinfo)
        {
            if (dinfo.Def == DamageDefOf.EMP)
            {
                var dmgAmount = dinfo.Amount;
                if (__instance.parent is Pawn p && (p.RaceProps?.IsFlesh ?? false))
                {
                    dmgAmount = Mathf.RoundToInt(dmgAmount * 0.25f);
                }
                var newDinfo = new DamageInfo(CE_DamageDefOf.Electrical, dmgAmount, 9999, // Hack to avoid double-armor application (EMP damage reduced -> proportional electric damage reduced again)
                                              dinfo.Angle, dinfo.Instigator, dinfo.HitPart, dinfo.Weapon, dinfo.Category, instigatorGuilty: dinfo.InstigatorGuilty);
                __instance.parent.TakeDamage(newDinfo);
            }
        }
    }
}
