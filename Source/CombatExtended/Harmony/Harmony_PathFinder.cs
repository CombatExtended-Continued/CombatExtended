using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static SightGrid eSightGrid;
        private static SightGrid uSightGrid;
        private static bool crouching;
        private static bool nightTime;

        internal static bool Prefix(PathFinder __instance, ref PawnPath __result, IntVec3 start, LocalTargetInfo dest, TraverseParms traverseParms, PathEndMode peMode, out bool __state)
        {
            __state = false;
            if (traverseParms.pawn != null && traverseParms.pawn.Faction != null && traverseParms.pawn.RaceProps.Humanlike && traverseParms.pawn.RaceProps.intelligence == Intelligence.Humanlike)
            {
                __state = true;

                map = __instance.map;
                pawn = traverseParms.pawn;                
                nightTime = map.IsNightTime();
                dangerTracker = map.GetDangerTracker();
                lightingTracker = map.GetLightingTracker();

                if (!lightingTracker.IsNight)
                    lightingTracker = null;
                if (map.ParentFaction != pawn?.Faction)
                    turretTracker = map.GetComponent<TurretTracker>();

                SightTracker tracker = map.GetSightTracker();
                if (pawn?.Faction != null)
                    tracker.TryGetGrid(pawn, out eSightGrid);                                                            
                if (pawn.RaceProps.IsMechanoid || pawn.RaceProps.Insect)
                    uSightGrid = tracker.UniversalEnemies;

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
                __state = false;
                __result = PawnPath.NotFound;
                return false;
            }
            else
            {
                Reset();
                return true;
            }
        }

        public static void Postfix(PathFinder __instance, PawnPath __result, bool __state)
        {
            //if (__state && __result != PawnPath.NotFound && __result != null)
            //{
            //    for(int i = 0; i < __result.nodes.Count; i++)
            //    {                    
            //    }
            //}
            Reset();
        }

        public static void Reset()
        {
            map = null;
            turretTracker = null;
            uSightGrid = null;
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
            if (map != null)
            {
                int value = 0;
                if (turretTracker != null)
                    value += turretTracker.GetVisibleToTurret(index) ? 75 : 0;
                if (eSightGrid != null)               
                    value += (int)Mathf.Min(eSightGrid.GetVisibility(index) * 50, 500);
                if (uSightGrid != null)
                    value += (int)Mathf.Min(uSightGrid.GetVisibility(index) * 50, 350);
                
                if (value > 0)
                {
                    if (dangerTracker != null)
                        value += (int)(dangerTracker.DangerAt(index) * 75f);
                    if (nightTime && lightingTracker != null)
                        value += (int)(lightingTracker.CombatGlowAt(map.cellIndices.IndexToCell(index)) * 45f);
                }                                             
                return Mathf.Min(value, 700);
            }
            return 0;
        }       
    }
}