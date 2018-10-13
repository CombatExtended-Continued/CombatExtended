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
                && dinfo.Def.ExternalViolenceFor(dinfo.IntendedTarget) 
                && dinfo.Weapon != null 
                && (dinfo.Weapon.IsRangedWeapon || dinfo.Weapon.projectile is ProjectilePropertiesCE))  // Add a check for CE projectiles since we're using them as weaponGear to pass data to our ArmorUtility
            {
                IntVec3 position = __instance.Wearer.Position;
                Map map = __instance.Wearer.Map;
                float statValue = __instance.GetStatValue(StatDefOf.SmokepopBeltRadius, true);
                DamageDef smoke = DamageDefOf.Smoke;
                Thing instigator = null;
                ThingDef gas_Smoke = ThingDefOf.Gas_Smoke;
                GenExplosion.DoExplosion(position, map, statValue, smoke, instigator, -1, -1, null, null, gas_Smoke, null, null, 1f, 1, false, null, 0f, 1, 0f, false);
                __instance.Destroy(DestroyMode.Vanish);
            }
            return false;
        }
    }
}
