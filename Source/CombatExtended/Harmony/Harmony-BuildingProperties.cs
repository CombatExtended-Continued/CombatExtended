using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(BuildingProperties), "SpecialDisplayStats")]
    static class Harmony_BuildingProperties_SpecialDisplayStats_Patch
    {
        public static void Postfix(BuildingProperties __instance, ref IEnumerable<StatDrawEntry> __result, ThingDef parentDef, StatRequest req)
        {
            var turretGunDef = __instance.turretGunDef;

            if (turretGunDef != null)
            {
                var statRequestGun = StatRequest.For(turretGunDef, null);

                var newStats1 = DefDatabase<StatDef>.AllDefs
                    .Where(x => x.category == StatCategoryDefOf.Weapon
                        && x.Worker.ShouldShowFor(statRequestGun)
                        && !x.Worker.IsDisabledFor(req.Thing)
                        && !(x.Worker is StatWorker_MeleeStats))
                    .Select(x => new StatDrawEntry(StatCategoryDefOf.Weapon, x, turretGunDef.GetStatValueAbstract(x), statRequestGun, ToStringNumberSense.Undefined))
                    .Where(x => x.ShouldDisplay)
                    .Concat(turretGunDef.SpecialDisplayStats(statRequestGun));

                __result = __result.Concat(newStats1);
            }
        }
    }
}
