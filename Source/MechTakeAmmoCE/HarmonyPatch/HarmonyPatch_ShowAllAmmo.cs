using CombatExtended;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MTA.Patch
{
    [HarmonyPatch(typeof(Command_Reload), "BuildAmmoOptions")]
    internal class HarmonyPatch_ShowAllAmmo
    {
        public static bool Prefix(Command_Reload __instance, ref List<FloatMenuOption> __result)
        {
            CompMechAmmo mechAmmo = __instance.compAmmo?.Holder?.GetComp<CompMechAmmo>();
            if (mechAmmo == null)
            {
                return true;
            }

            __result = __instance.compAmmo.BuildAmmoOptions(mechAmmo);
            return false;
        }
    }
}
