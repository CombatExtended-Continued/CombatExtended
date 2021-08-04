using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(PathFinder), nameof(PathFinder.FindPath), new[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(TraverseParms), typeof(PathEndMode), typeof(PathFinderCostTuning) })]
    internal static class Harmony_PathFinder_FindPath
    {
        private static Map map;
        private static LightingTracker lightingTracker;
        private static DangerTracker dangerTracker;
        private static bool combaten;
        private static bool crouching;

        internal static bool Prefix(PathFinder __instance, ref PawnPath __result, IntVec3 start, LocalTargetInfo dest, TraverseParms traverseParms, PathEndMode peMode)
        {
            map = __instance.map;
            dangerTracker = __instance.map.GetDangerTracker();
            lightingTracker = __instance.map.GetLightingTracker();
            combaten = traverseParms.pawn?.jobs?.curJob?.def.alwaysShowWeapon ?? false;
            var pawn = traverseParms.pawn;
            var comp = pawn?.TryGetComp<CompSuppressable>();

            // Run normal if we're not being suppressed, running for cover, crouch-walking or not actually moving to another cell
            if (comp == null
                || !comp.isSuppressed
                || comp.IsCrouchWalking
                || pawn.CurJob?.def == CE_JobDefOf.RunForCover
                || start == dest.Cell && peMode == PathEndMode.OnCell)
            {
                crouching = comp?.IsCrouchWalking ?? false;
                return true;
            }

            // Make all destinations unreachable
            __result = PawnPath.NotFound;
            return false;
        }

        /*
         * Search for the vairable that is initialized by the value from the avoid grid or search for
         * ((i > 3) ? num9 : num8) + num15;
         * 
         * DISABLED: reason is it need a bit more constant tuning but never the less the code is 100% working
         */
        //public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        //{
        //    List<CodeInstruction> codes = instructions.ToList();
        //    bool finished = false;
        //    for (int i = 0; i < codes.Count; i++)
        //    {
        //        if (!finished)
        //        {
        //            if (codes[i].opcode == OpCodes.Stloc_S && codes[i].operand is LocalBuilder builder1 && builder1.LocalIndex == 46)
        //            {
        //                finished = true;
        //                yield return new CodeInstruction(OpCodes.Ldloc_S, 43).MoveLabelsFrom(codes[i]).MoveBlocksFrom(codes[i]);
        //                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_PathFinder_FindPath), nameof(Harmony_PathFinder_FindPath.GetDangerAt)));
        //                yield return new CodeInstruction(OpCodes.Add);
        //            }
        //        }
        //        yield return codes[i];
        //    }
        //    if (finished)
        //        Log.Message("CE: Patched pather!");
        //}

        //private static int GetDangerAt(int index)
        //{
        //    int value = (int)dangerTracker.DangerAt(index) * 4000;
        //    if (crouching)
        //        value /= 2;
        //    if (combaten && lightingTracker.IsNight)
        //        value += (int)lightingTracker.CombatGlowAt(map.cellIndices.IndexToCell(index)) * 2000;
        //    return value;
        //}
    }
}