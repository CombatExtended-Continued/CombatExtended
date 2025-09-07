using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace CombatExtended.HarmonyCE
{
    // Need to patch all methods that can modify pawn's inventory to refresh CompInventory cache when it happens
    public class Harmony_ThingOwner_NotifyAdded_Patch
    {
        public static void Postfix(ThingOwner __instance, Thing item)
        {
            if (item != null)
            {
                CE_Utility.TryUpdateInventory(__instance);
            }
        }
    }

    public class Harmony_ThingOwner_NotifyAddedAndMergedWith_Patch
    {
        public static void Postfix(ThingOwner __instance, Thing item, int mergedCount)
        {
            if (item != null && mergedCount != 0)
            {
                CE_Utility.TryUpdateInventory(__instance);
            }
        }
    }

    public class Harmony_ThingOwner_Take_Patch
    {
        public static void Postfix(ThingOwner __instance, Thing __result)
        {
            if (__result != null)
            {
                CE_Utility.TryUpdateInventory(__instance);
            }
        }
    }

    public class Harmony_ThingOwner_NotifyRemoved_Patch
    {
        public static void Postfix(ThingOwner __instance, Thing item)
        {
            if (item != null)
            {
                CE_Utility.TryUpdateInventory(__instance);
            }
        }
    }
}
