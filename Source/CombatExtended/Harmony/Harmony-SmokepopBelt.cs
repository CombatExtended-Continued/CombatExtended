using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using Harmony;

namespace CombatExtended.Harmony
{
    // FIXME: Destructive detour
    [HarmonyPatch(typeof(SmokepopBelt), "CheckPreAbsorbDamage")]
    static class Harmony_SmokepopBelt
    {
        public static bool Prefix(SmokepopBelt __instance, DamageInfo dinfo)
        {
            if (!dinfo.Def.isExplosive 
                && dinfo.Def.harmsHealth 
                && dinfo.Def.externalViolence 
                && dinfo.WeaponGear != null 
                && (dinfo.WeaponGear.IsRangedWeapon || dinfo.WeaponGear.projectile is ProjectilePropertiesCE))  // Add a check for CE projectiles since we're using them as weaponGear to pass data to our ArmorUtility
            {
                ThingDef gas_Smoke = ThingDefOf.Gas_Smoke;
                GenExplosion.DoExplosion(__instance.Wearer.Position, __instance.Wearer.Map, __instance.GetStatValue(StatDefOf.SmokepopBeltRadius, true), DamageDefOf.Smoke, null, null, null, null, gas_Smoke, 1f, 1, false, null, 0f, 1);
                __instance.Destroy(DestroyMode.Vanish);
            }
            return false;
        }
    }
}
