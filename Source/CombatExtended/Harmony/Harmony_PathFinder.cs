using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(PathFinder), nameof(PathFinder.FindPath), new[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(TraverseParms), typeof(PathEndMode), typeof(PathFinderCostTuning) })]
    internal static class Harmony_PathFinder
    {
        internal static bool Prefix(ref PawnPath __result, IntVec3 start, LocalTargetInfo dest, TraverseParms traverseParms, PathEndMode peMode)
        {
            var pawn = traverseParms.pawn;
            var comp = pawn?.TryGetComp<CompSuppressable>();

            // Run normal if we're not being suppressed, running for cover, crouch-walking or not actually moving to another cell
            if (comp == null
                || !comp.isSuppressed
                || comp.IsCrouchWalking
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