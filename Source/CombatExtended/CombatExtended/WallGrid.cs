using System;
using Verse;

namespace CombatExtended
{
    public class WallGrid : MapComponent
    {
        private readonly bool[][] grid;

        public WallGrid(Map map) : base(map)
        {
            grid = new bool[map.cellIndices.mapSizeX][];
            for (int i = 0; i < map.cellIndices.mapSizeX; i++)
                grid[i] = new bool[map.cellIndices.mapSizeZ];
        }

        public bool this[IntVec3 cell]
        {
            get => grid[cell.x][cell.z];
            set => grid[cell.x][cell.z] = value;
        }

        public bool this[int index]
        {
            get => this[map.cellIndices.IndexToCell(index)];
            set => this[map.cellIndices.IndexToCell(index)] = value;
        }
    }
}

