using HarmonyLib;
using Verse;
using Verse.AI;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(PathFinder), nameof(PathFinder.FindPathNow), new[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(TraverseParms), typeof(PathFinderCostTuning), typeof(PathEndMode), typeof(PathRequest.IPathGridCustomizer) })] // pathfinding is treaded now, not touching it for the time being
    internal static class Harmony_PathFinder_FindPathNow
    {

        internal static bool Prefix(PathFinder __instance, ref PawnPath __result, IntVec3 start, LocalTargetInfo target, TraverseParms traverseParms, PathEndMode peMode)
        {
            var pawn = traverseParms.pawn;
            var comp = pawn?.TryGetComp<CompSuppressable>();

            // Run normal if we're not being suppressed, running for cover, crouch-walking or not actually moving to another cell
            if (comp == null
                    || !comp.isSuppressed
                    || comp.IsCrouchWalking
                    || pawn.CurJob?.def == CE_JobDefOf.RunForCover
                    || start == target.Cell && peMode == PathEndMode.OnCell)
            {
                return true;
            }

            // Make all destinations unreachable
            __result = PawnPath.NotFound;
            return false;
        }

    }
}
