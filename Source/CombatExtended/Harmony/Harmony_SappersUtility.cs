using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(SappersUtility), "CanMineReasonablyFast")]
    internal static class Harmony_SappersUtility
    {
        internal static bool Prefix(Pawn p, ref bool __result)
        {
            if (p.RaceProps.IsMechanoid)
            {
                __result = true;
                return false;
            }

            return true;
        }
    }
}