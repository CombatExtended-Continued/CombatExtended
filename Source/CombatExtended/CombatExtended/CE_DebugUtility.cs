using System;
using RimWorld.Planet;
using Verse;

namespace CombatExtended
{
    public static class CE_DebugUtility
    {
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
    }
}
