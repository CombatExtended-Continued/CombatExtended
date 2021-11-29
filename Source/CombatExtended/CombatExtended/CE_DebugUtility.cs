using System;
using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace CombatExtended
{
    public static class CE_DebugUtility
    {
        //private const int LOSSHADOWRADIUS = 70;

        //private static readonly List<Pair<IntVec3, float>> shadowLosCells = new List<Pair<IntVec3, float>>();

        [CE_DebugTooltip(CE_DebugTooltipType.Map)]
        public static string CellPositionTip(Map map, IntVec3 cell)
        {
            return $"Cell: ({cell.x}, {cell.z})";
        }

        [CE_DebugTooltip(CE_DebugTooltipType.World)]
        public static string TileIndexTip(World world, int tile)
        {
            return $"Tile index: {tile}";
        }        

        //[CE_DebugTooltip(CE_DebugTooltipType.Map)]
        //public static string ShadowGrid(Map map, IntVec3 center)
        //{
        //    if (!Controller.settings.DebugDrawLOSShadowGrid)
        //        return null;
        //    if (Find.TickManager.Paused)
        //        return $"LOS shaodw grid: Please unpause!";
        //    if (GenTicks.TicksGame % 15 != 0)
        //        return $"LOS shaodw grid radius: {LOSSHADOWRADIUS}";
        //    shadowLosCells.Clear();
        //    float maxValue = -1;
        //    //
        //    SightTracker sightTracker = map.GetComponent<SightTracker>();
        //    // 
        //    TurretTracker turretTracker = map.GetComponent<TurretTracker>();
        //
        //    if (center.InBounds(map))
        //    {
        //        foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 64, true))
        //        {                    
        //            if (cell.InBounds(map))
        //            {                        
        //                float value = 0;

        //                float h = sightTracker.Friendly[cell];
        //                if (h >= 0)
        //                    value += h;

        //                float t = turretTracker.GetTurretsVisibleCount(map.cellIndices.CellToIndex(cell));
        //                if (t >= 0)
        //                    value += t;

        //                if (maxValue < t)
        //                    maxValue = t;

        //                if (value > 0)
        //                    shadowLosCells.Add(new Pair<IntVec3, float>(cell, value));
        //            }
        //        }
        //        if (maxValue != 0)
        //        {
        //            foreach (Pair<IntVec3, float> c in shadowLosCells)
        //                map.debugDrawer.FlashCell(c.first, c.second / maxValue, $"{Math.Round(c.second / maxValue, 2)}", 15);
        //        }
        //    }
        //    shadowLosCells.Clear();
        //    return $"LOS shaodw grid radius: {LOSSHADOWRADIUS}";
        //}
    }
}
