using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;
using RimWorld.QuestGen;

namespace CombatExtended.HarmonyCE
{
    /* Fix for the "Failed to find CombatExtended.AmmoDef named Leather_Human. There are 901 defs of this type loaded." error.
	 * This occurs when value1 contains a ThingDef with a custom class (such as CombatExtended.AmmoDef), and value2 contains a 
	 * string identifier for a ThingDef. Due to how Verse.ConvertHelper is set up, it assumes that strings can always be converted
	 * to any object class, which isn't the case with the TradeRequest quest, where value1 will potentially load a CE ammo def,
	 * and value2 contains a string identifier for ThingDef.Leather_Human. As the latter can't be cast into CombatExtended.AmmoDef,
	 * the error occurs on database lookup.
	 * Issue has been mentioned to Ludeon to verify whether this is something to be fixed on their end.
	 * For now, the prefix below will prevent the error.
	 */
    [HarmonyPatch(typeof(QuestNodeEqualUtility), "Equal")]
    public static class Harmony_QuestNodeEqualUtility_PreventThingDefToAmmoDefCast
    {
        public static bool Prefix(object value1, object value2, Type compareAs, ref bool __result)
        {
            if (value1 is AmmoDef && value2 is string value2String)
            {
                var lookupAttempt = DefDatabase<AmmoDef>.GetNamed(value2String, false);
                if (lookupAttempt == null)
                {
                    __result = false;
                    return false;
                }
            }
            return true;
        }
    }

}
