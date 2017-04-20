using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RimWorld;
using Verse;
using UnityEngine;
using HugsLib.Source.Detour;

namespace CombatExtended.Detours
{
    internal static class Detours_HediffComp_GetsOld
    {
        [DetourMethod(typeof(HediffComp_GetsOld), "CompPostInjuryHeal")]
        internal static void CompPostInjuryHeal(this HediffComp_GetsOld _this, float amount)
        {
            if (_this.oldDamageThreshold >= 9000f || _this.IsOld)
            {
                return;
            }
            if (_this.parent.Severity <= _this.oldDamageThreshold)
            {
                float chance = 1;
                HediffComp_TendDuration compTended = _this.parent.TryGetComp<HediffComp_TendDuration>();
                if (compTended != null)
                {
                    chance = Mathf.Clamp01(chance / Mathf.Pow(compTended.tendQuality + 0.75f, 2));
                }
                if (Rand.Value < chance)
                {
                    _this.parent.Severity = _this.oldDamageThreshold;
                    _this.IsOld = true;
                }
                else
                {
                    _this.oldDamageThreshold = 9999f;
                }
            }
        }
    }
}
