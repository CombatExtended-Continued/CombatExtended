using System;
using RimWorld;
using Verse;
using UnityEngine;
using System.Runtime.InteropServices;

namespace CombatExtended
{
    /*
     * -----------------------------
     *
     *
     * ------ Important note -------
     * 
     * when casting update the grid at a regualar intervals for a pawn/Thing or risk exploding value issues.
     */
    public class SightGrid
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct SightRecord
        {
            public int sig;            
            public int value;
            public int count;
            public int countPrev;
        }

        private float updateInterval;
        private int sig = 13;       
        private SightRecord[] grid;
        private CellIndices indices;
        private Faction faction;
        private Map map;

        public Faction Faction
        {
            get => faction;
        }

        public SightGrid(Map map, Faction faction, float updateInterval)
        {
            grid = new SightRecord[map.cellIndices.NumGridCells];            
            this.updateInterval = updateInterval;
            this.map = map;
            this.faction = faction;
            indices = map.cellIndices;
            for (int i = 0; i < grid.Length; i++)
            {
                grid[i] = new SightRecord()
                {
                    sig = -1,                    
                    value = 0
                };
            }
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
                    SightRecord record = grid[index];
                    if(record.value - GenTicks.TicksGame >= -updateInterval)
                        return Math.Max(record.count, record.countPrev);
                }
                return 0;
            }
            set
            {
                if (index >= 0 && index < indices.NumGridCells)
                {
                    SightRecord record = grid[index];
                    if (record.sig != sig)
                    {
                        float t = record.value - GenTicks.TicksGame;
                        if (t > 0.0f)
                        {
                            record.count += 1;
                        }
                        else if (t >= -updateInterval)
                        {
                            record.value = (int)(GenTicks.TicksGame + updateInterval);
                            record.countPrev = record.count;
                            record.count = 1;
                        }
                        else
                        {
                            record.value = (int)(GenTicks.TicksGame + updateInterval);
                            record.countPrev = 0;
                            record.count = 1;
                        }
                        record.sig = sig;
                        grid[index] = record;
                    }
                }
            }
        }

        public void Next() => sig += 1;
    }
}

