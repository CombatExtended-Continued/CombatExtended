using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE;
[HarmonyPatch(typeof(Pawn_MechanitorTracker), nameof(Pawn_MechanitorTracker.CanCommandTo))]
public static class Harmony_MechanitorTracker_CanCommandTo
{
    static void Postfix(Pawn_MechanitorTracker __instance, ref bool __result, LocalTargetInfo target, Pawn ___pawn)
    {
        if (___pawn != null)
        {
            __result = (float)___pawn.Position.DistanceToSquared(target.Cell) < 1927.21;
        }
    }
}
