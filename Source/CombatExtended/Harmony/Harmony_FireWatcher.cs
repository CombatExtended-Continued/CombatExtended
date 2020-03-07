using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(FireWatcher), "get_LargeFireDangerPresent")]
    internal static class Harmony_FireWatcher
    {
        private const float DangerThreshold = 450f;

        internal static void Postfix(FireWatcher __instance, ref bool __result)
        {
            __result = __instance.FireDanger > DangerThreshold;
        }
    }
}