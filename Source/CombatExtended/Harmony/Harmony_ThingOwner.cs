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

    //[HarmonyPatch(typeof(ThingOwner), "TryAdd", new Type[] { typeof(Thing), typeof(bool) })]
    public class Harmony_ThingOwner_TryAdd_Patch
    {
        public static void Postfix(ThingOwner __instance, bool __result)
        {
            if (__result)
            {
                CE_Utility.TryUpdateInventory(__instance);
            }
        }
    }

    //[HarmonyPatch(typeof(ThingOwner), "Take", new Type[] { typeof(Thing), typeof(int) })]
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

    //[HarmonyPatch(typeof(ThingOwner), "Remove", new Type[] { typeof(Thing) })]
    public class Harmony_ThingOwner_Remove_Patch
    {
        public static void Postfix(ThingOwner __instance, bool __result)
        {
            if (__result)
            {
                CE_Utility.TryUpdateInventory(__instance);
            }
        }
    }
}
