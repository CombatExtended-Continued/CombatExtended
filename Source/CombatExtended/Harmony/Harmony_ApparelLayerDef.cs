using System;
using System.Collections;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Verse;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(ApparelLayerDef), nameof(ApparelLayerDef.IsUtilityLayer), MethodType.Getter)]
    public class Harmony_ApparelLayerDef
    {
        /*
         * Used to make backpack a utility layer
         *
         * Note this is experimental
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Postfix(ApparelLayerDef __instance, ref bool __result)
        {
            __result = __result || __instance == CE_ApparelLayerDefOf.Backpack;
        }
    }
}
