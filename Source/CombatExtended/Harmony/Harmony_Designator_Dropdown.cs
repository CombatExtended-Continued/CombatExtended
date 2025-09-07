using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CombatExtended
{
    [HarmonyLib.HarmonyPatch(typeof(Designator_Dropdown), nameof(Designator.Visible), HarmonyLib.MethodType.Getter)]
    internal static class Harmony_Designator_Dropdown
    {
        static void Postfix(Designator_Dropdown __instance, bool __result)
        {
            if (__result && !__instance.activeDesignator.Visible)
            {
                var visibleElement = __instance.elements.FirstOrDefault(x => x.Visible);
                if (visibleElement != null)
                {
                    __instance.SetActiveDesignator(visibleElement, false);
                }
            }
        }
    }
}
