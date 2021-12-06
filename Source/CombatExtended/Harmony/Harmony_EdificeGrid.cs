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
        public static WallGrid grid;

        public static MethodBase mCellToIndex = AccessTools.Method(typeof(CellIndices), nameof(CellIndices.CellToIndex), new[] { typeof(IntVec3) });
        public static MethodBase mCellToIndex2 = AccessTools.Method(typeof(CellIndices), nameof(CellIndices.CellToIndex), new[] { typeof(int), typeof(int) });

        [HarmonyPatch(typeof(EdificeGrid), nameof(EdificeGrid.Register))]
        public static class Harmony_EdificeGrid_RegisterInCell
        {
            public static void Prefix(EdificeGrid __instance, Building ed)
            {
                if (!ed.CanBeSeenOver())
                    grid = __instance.map.GetComponent<WallGrid>();               
            }

            public static void Postfix()
            {
                grid = null;
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codes = instructions.ToList();
                bool finished = false;
                for (int i = 0; i < codes.Count; i++)
                {
                    yield return codes[i];
                    if (!finished)
                    {
                        if (codes[i].OperandIs(mCellToIndex))
                        {
                            finished = true;
                            yield return new CodeInstruction(OpCodes.Dup);
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_ThingGrid), nameof(Harmony_ThingGrid.Set)));
                        }
                    }                    
                }                
            }
        }

        [HarmonyPatch(typeof(EdificeGrid), nameof(EdificeGrid.DeRegister))]
        public static class Harmony_EdificeGrid_DeregisterInCell
        {
            public static void Prefix(EdificeGrid __instance, Building ed)
            {
                if (!ed.CanBeSeenOver())
                    grid = __instance.map.GetComponent<WallGrid>();                
            }

            public static void Postfix()
            {
                grid = null;
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codes = instructions.ToList();
                bool finished = false;
                for(int i = 0; i < codes.Count; i++)
                {
                    yield return codes[i];
                    if (!finished)
                    {
                        if (codes[i].OperandIs(mCellToIndex2))
                        {
                            finished = true;
                            yield return new CodeInstruction(OpCodes.Dup);
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_ThingGrid), nameof(Harmony_ThingGrid.UnSet)));
                        }
                    }                    
                }                
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set(int index)
        {            
            if (grid != null)            
                grid[index] = true;                            
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnSet(int index)
        {
            if (grid != null)            
                grid[index] = false;                            
        }
    }
}

