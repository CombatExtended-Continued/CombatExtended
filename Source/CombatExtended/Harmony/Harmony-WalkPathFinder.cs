using System;
using System.Collections.Generic;
using Harmony;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended.Harmony
{
    [HarmonyPatch(typeof(PathFinder), "FindPath", new[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(TraverseParms), typeof(PathEndMode) })]
    internal static class Harmony_WalkPathFinder
    {
        internal static bool Prefix(ref PawnPath __result, IntVec3 start, LocalTargetInfo dest, TraverseParms traverseParms, PathEndMode peMode)
        {
            var pawn = traverseParms.pawn;
            var comp = pawn?.TryGetComp<CompSuppressable>();

            // Run normal if we're not being suppressed, running for cover or not actually moving to another cell
            if (comp == null
                || !comp.isSuppressed
                || pawn.CurJob?.def == CE_JobDefOf.RunForCover
                || start == dest.Cell && peMode == PathEndMode.OnCell)
            {
                return true;
            }

            // Make all destinations unreachable
            __result = PawnPath.NotFound;
            return false;
        }
    }
}