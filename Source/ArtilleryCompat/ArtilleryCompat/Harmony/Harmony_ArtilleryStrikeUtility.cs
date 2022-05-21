using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;
using VFESecurity;
using CombatExtended;
using RimWorld;

namespace CombatExtended.Compatibility.Artillery
{
    [HarmonyPatch(typeof(ArtilleryStrikeUtility), "SetCache")]
    public class Harmony_ArtilleryStrikeUtility_SetCache {
        public static void Postfix(List<ThingDef> ___allowedEnemyShellDefs) {
            ___allowedEnemyShellDefs.Clear();
            foreach (AmmoDef t in DefDatabase<AmmoDef>.AllDefsListForReading) {
                if (t.isMortarAmmo) {
                    if (t.detonateProjectile?.projectile?.damageDef?.harmsHealth ?? false) {
                        ___allowedEnemyShellDefs.Add(t);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(ArtilleryStrikeUtility), "GetRandomShellFor")]
    public class Harmony_ArtilleryStrikeUtility_GetRandomShellFor {
        private static bool CanAcceptAmmo(CompProperties_AmmoUser prop, AmmoDef shellDef) => shellDef.AmmoSetDefs.Contains(prop.ammoSet);

        public static bool Prefix(ThingDef artilleryGunDef, FactionDef faction, out ThingDef __result, List<ThingDef> ___allowedEnemyShellDefs) {
            var candidates = (from s in ___allowedEnemyShellDefs
                                         where s.techLevel <= faction.techLevel && CanAcceptAmmo(artilleryGunDef.GetCompProperties<CompProperties_AmmoUser>(), (AmmoDef)s)
                                         select s);
            __result = candidates.RandomElementByWeight((s) => s.BaseMarketValue * ((int)s.techLevel) + 1);
            return false;
        }
    }
}
