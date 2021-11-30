using System;
using System.Collections.Generic;
using RimWorld.Planet;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public static class CE_DebugUtility
    {
        // private const int LOSSHADOWRADIUS = 70;
        // private static readonly List<Pair<IntVec3, float>> shadowLosCells = new List<Pair<IntVec3, float>>();

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
       
        [CE_DebugTooltip(CE_DebugTooltipType.Map)]
        public static string CoverRatingFriendly(Map map, IntVec3 cell)
        {
            if (!cell.InBounds(map))
                return null;
            SightGrid grid = map.GetComponent<SightTracker>().Friendly;
            return $"<color=green>Friendly</color>\n" +
                   $"Sight rating: {grid.GetCellSightCoverRating(cell)}\n" +
                   $"Has cover: {grid.HasCover(cell)}";
        }

        [CE_DebugTooltip(CE_DebugTooltipType.Map)]
        public static string CoverRatingHostile(Map map, IntVec3 cell)
        {
            if (!cell.InBounds(map))
                return null;
            SightGrid grid = map.GetComponent<SightTracker>().Hostile;
            float enemies = grid[cell];
            float val = 0f;
            if (enemies > 0)
            {
                val = (64 - Mathf.Min(grid.GetDirectionAt(cell).magnitude / (0.5f * enemies), 64)) * enemies / 2f;
                if (val > 0)
                    val = Mathf.Log(val, 2f);
            }
            return $"<color=red>Hostile</color>\n" +
                   $"Sight rating: {grid.GetCellSightCoverRating(cell)} {val}\n" +
                   $"Has cover: {grid.HasCover(cell)}";
        }
    }
}
