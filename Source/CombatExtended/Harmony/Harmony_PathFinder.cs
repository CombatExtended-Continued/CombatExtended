using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(PathFinder), nameof(PathFinder.FindPath), new[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(TraverseParms), typeof(PathEndMode), typeof(PathFinderCostTuning) })]
    internal static class Harmony_PathFinder_FindPath
    {
        private static Pawn pawn;
        private static Map map;
        private static LightingTracker lightingTracker;
        private static DangerTracker dangerTracker;
        private static TurretTracker turretTracker;
        private static SightGrid sightGrid;                
        private static bool crouching;

        internal static bool Prefix(PathFinder __instance, ref PawnPath __result, IntVec3 start, LocalTargetInfo dest, TraverseParms traverseParms, PathEndMode peMode)
        {            
            map = __instance.map;
            pawn = traverseParms.pawn;
            dangerTracker = __instance.map.GetDangerTracker();
            
            if (!lightingTracker.IsNight)
                lightingTracker = null;                               
            if (map.ParentFaction != pawn?.Faction)
                turretTracker = map.GetComponent<TurretTracker>();
            if (pawn?.Faction != null)            
                map.GetComponent<SightTracker>().TryGetGrid(pawn, out sightGrid);

            // Run normal if we're not being suppressed, running for cover, crouch-walking or not actually moving to another cell
            CompSuppressable comp = pawn?.TryGetComp<CompSuppressable>();
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

        public static void Postfix()
        {
            turretTracker = null;
            sightGrid = null;
            pawn = null;
            dangerTracker = null;
            lightingTracker = null;
        }

        /*
         * Search for the vairable that is initialized by the value from the avoid grid or search for
         * ((i > 3) ? num9 : num8) + num15;
         *          
         */
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = instructions.ToList();
            bool finished = false;
            for (int i = 0; i < codes.Count; i++)
            {
                if (!finished)
                {
                    if (codes[i].opcode == OpCodes.Stloc_S && codes[i].operand is LocalBuilder builder1 && builder1.LocalIndex == 46)
                    {
                        finished = true;
                        yield return new CodeInstruction(OpCodes.Ldloc_S, 43).MoveLabelsFrom(codes[i]).MoveBlocksFrom(codes[i]);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_PathFinder_FindPath), nameof(Harmony_PathFinder_FindPath.GetDangerAt)));
                        yield return new CodeInstruction(OpCodes.Add);
                    }
                }
                yield return codes[i];
            }
            if (finished)
                Log.Message("CE: Patched pather!");
        }

        private static int GetDangerAt(int index)
        {
            int value = 0;            
            if (lightingTracker != null)
                value += (int)(lightingTracker.CombatGlowAt(map.cellIndices.IndexToCell(index)) * 300);
            if (turretTracker != null)
                value += turretTracker.GetVisibleToTurret(index) ? 1000 : 0;
            if (sightGrid != null)
                value += (int)Mathf.Max(sightGrid[index] * 800, 3800);
            if (crouching)
                value /= 2;                            
            return value;
        }
    }
}