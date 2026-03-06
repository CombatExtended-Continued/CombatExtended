using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CombatExtended.HarmonyCE;
[HarmonyPatch(typeof(CompProjectileInterceptor), nameof(CompProjectileInterceptor.PostPreApplyDamage))]
internal static class Harmony_CompProjectileInterceptor_PostPreApplyDamage
{
    public static bool Prefix(CompProjectileInterceptor __instance)
    {
        return __instance.Active;
    }

}
