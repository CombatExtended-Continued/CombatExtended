using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;
using RimWorld;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(MechClusterGenerator), "GetBuildingDefsForCluster")]
    public static class Harmony_MechClusterGenerator
    {
		[HarmonyPostfix]
        public static void PostFix(List<ThingDef> __result)
        {
            if (Controller.settings.EnableAmmoSystem
                && __result.Any(x => x.building.IsTurret
                    && !x.building.IsMortar
                    && x.GetCompProperties<CompProperties_AmmoUser>()?.ammoSet != null))
            {
                __result.Add(DefDatabase<ThingDef>.GetNamed("CombatExtended_MechAmmoBeacon"));
            }
        }
    }
}
