using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(ListerThings), "EverListable")]
    internal static class Harmony_ListerThings_AllowSavingGas
    {
        internal static void Postfix(ref bool __result, ThingDef def, ListerThingsUse use)
        {
            __result = __result || def.category == ThingCategory.Gas;
            return;
        }
    }
}
