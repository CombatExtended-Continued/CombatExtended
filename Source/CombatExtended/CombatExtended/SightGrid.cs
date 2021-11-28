using System;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class SightGrid
    {
        private const int SHADOW_DANGETICKS = 1100;

        private int sig = 13;
        private int[] sigs;
        private int[] grid;
        private CellIndices indices;
        private Faction faction;
        private Map map;

        public Faction Faction
        {
            get => faction;
        }

        public SightGrid(Map map, Faction faction)
        {
            grid = new int[map.cellIndices.NumGridCells];
            sigs = new int[map.cellIndices.NumGridCells];
            this.map = map;
            this.faction = faction;
            indices = map.cellIndices;
        }

        public float this[IntVec3 cell]
        {
            get => this[indices.CellToIndex(cell)];
            set => this[indices.CellToIndex(cell)] = value;
        }

        public float this[int index]
        {
            get
            {
                if (index >= 0 && index < indices.NumGridCells)
                {
                    int val = grid[index] - GenTicks.TicksGame;
                    if (val == 0)
                        return 1f;
                    else
                        return Mathf.Clamp(Mathf.CeilToInt(val / 900f), 0f, 10f);
                }
                return 0;
            }
            set
            {
                if (index >= 0 && index < indices.NumGridCells && sigs[index] != sig)
                {
                    sigs[index] = sig;
                    if (grid[index] - GenTicks.TicksGame < 0)
                        grid[index] = GenTicks.TicksGame + Mathf.CeilToInt(value);
                    else
                        grid[index] += Mathf.CeilToInt(value);                    
                }
            }
        }

        public void Next() => sig += 1;
    }
}

