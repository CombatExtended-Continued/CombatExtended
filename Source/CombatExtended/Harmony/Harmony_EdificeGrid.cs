using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Reflection.Emit;

namespace CombatExtended.HarmonyCE
{
    public static class Harmony_ThingGrid
    {
        public static SightTracker sightTracker;

        public static MethodBase mCellToIndex = AccessTools.Method(typeof(CellIndices), nameof(CellIndices.CellToIndex));

        [HarmonyPatch(typeof(EdificeGrid), nameof(EdificeGrid.Register))]
        public static class Harmony_EdificeGrid_RegisterInCell
        {
            public static void Postfix(EdificeGrid __instance, Building ed)
            {
                if (ed.CanBeSeenOver())
                    sightTracker = __instance.map.GetSightTracker();
            }

            public static void Postfix()
            {
                sightTracker = null;
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codes = instructions.ToList();
                bool finished = false;
                for (int i = 0; i < codes.Count; i++)
                {
                    if (!finished)
                    {
                        if (codes[i].OperandIs(mCellToIndex))
                        {
                            yield return new CodeInstruction(OpCodes.Dup);
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_ThingGrid), nameof(Harmony_ThingGrid.Set)));
                        }
                    }
                    yield return codes[i];
                }
            }
        }

        [HarmonyPatch(typeof(EdificeGrid), nameof(EdificeGrid.DeRegister))]
        public static class Harmony_EdificeGrid_DeregisterInCell
        {
            public static void Prefix(EdificeGrid __instance, Building ed)
            {
                if(ed.CanBeSeenOver())
                    sightTracker = __instance.map.GetSightTracker();
            }

            public static void Postfix()
            {
                sightTracker = null;
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codes = instructions.ToList();
                bool finished = false;
                for(int i = 0; i < codes.Count; i++)
                {
                    if (!finished)
                    {
                        if (codes[i].OperandIs(mCellToIndex))
                        {
                            yield return new CodeInstruction(OpCodes.Dup);
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_ThingGrid), nameof(Harmony_ThingGrid.UnSet)));
                        }
                    }
                    yield return codes[i];
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set(IntVec3 cell)
        {
            sightTracker?.Notify_WallAdded(cell);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnSet(IntVec3 cell)
        {
            sightTracker?.Notify_WallRemoved(cell);
        }
    }
}

