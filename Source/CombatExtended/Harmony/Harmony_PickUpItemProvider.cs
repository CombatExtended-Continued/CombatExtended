using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE;

[HarmonyPatch(typeof(FloatMenuOptionProvider_PickUpItem), nameof(FloatMenuOptionProvider_PickUpItem.AppliesInt))]
public static class Harmony_PickUpItemProvider
{
    public static bool Prefix(FloatMenuOptionProvider_PickUpItem __instance, ref bool __result)
    {
        __result = false;
        return false;
    }
}
