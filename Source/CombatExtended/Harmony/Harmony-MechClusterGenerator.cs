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
    public class Harmony_MechClusterGenerator
    {
        public static void PostFix(ref List<ThingDef> __result)
        {
            if (Controller.settings.EnableAmmoSystem
                && __result.Any(x => x.building.IsTurret && !x.building.IsMortar))
            {
                __result.Add(DefDatabase<ThingDef>.GetNamed("CombatExtended_MechAmmoBeacon"));
            }
        }
    }
}
