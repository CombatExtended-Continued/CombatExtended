using HarmonyLib;
using Verse;
using RimWorld;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(SmokepopBelt), "Notify_BulletImpactNearby")]
    internal static class Harmony_SmokepopBelt
    {
        internal static bool Prefix(SmokepopBelt __instance)
        {
            if (__instance.Wearer == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
