using System;
using RimWorld;
using Verse;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Steamworks;

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
    [StaticConstructorOnStartup]
    public class SightGrid
    {
        [StructLayout(LayoutKind.Auto)]
        private struct SightRecord
        {
            public int expireAt;
            public short sig;                        
            public short count;
            public short countPrev;
            public Vector2 direction;
            public Vector2 directionPrev;            
        }

        [StructLayout(LayoutKind.Explicit, Size = 9)]
        private struct SightCoverRecord
        {
            [FieldOffset(0)] public int expireAt;
            [FieldOffset(4)] public float value;
            [FieldOffset(8)] public bool hasCover;            
        }

        private IntVec3 center;
        private float updateInterval;
        private short sig = 13;
        private CellIndices cellIndices;
        private SightRecord[] sightArray;
        private SightCoverRecord[] coverArray;
        private Faction faction;
        private Map map;
        private int mapSizeX;
        private int mapSizeZ;
        private int mapCellNum;

        public Faction Faction
        {
            get => faction;
        }

        public SightGrid(Map map, Faction faction, float updateInterval)
        {
            cellIndices = map.cellIndices;
            mapSizeX = (int)map.Size.x;
            mapSizeZ = (int)map.Size.z;            
            mapCellNum = mapSizeX * mapSizeZ;            
            sightArray = new SightRecord[map.cellIndices.NumGridCells];
            coverArray = new SightCoverRecord[map.cellIndices.NumGridCells];
            this.updateInterval = updateInterval;
            this.map = map;
            this.faction = faction;            
            for (int i = 0; i < sightArray.Length; i++)
            {
                coverArray[i] = new SightCoverRecord()
                {
                    expireAt = -1,
                    value = 0f,
                };
                sightArray[i] = new SightRecord()
                {
                    sig = -1,
                    expireAt = 0,
                    direction = Vector3.zero
                };
            }
        }

        public float this[IntVec3 cell]
        {
            get => this[cellIndices.CellToIndex(cell)];            
        }

        public float this[int index]
        {
            get
            {
                if (index >= 0 && index < mapCellNum)
                {
                    SightRecord record = sightArray[index];
                    if(record.expireAt - GenTicks.TicksGame >= -updateInterval)
                        return Math.Max(record.count, record.countPrev);
                }
                return 0;
            }
        }

        public void Set(IntVec3 cell, int num = 1) => Set(cellIndices.CellToIndex(cell), num);
        public void Set(int index, int num = 1)
        {
            if (index >= 0 && index < mapCellNum)
            {
                SightRecord record = sightArray[index];
                IntVec3 cell = cellIndices.IndexToCell(index);
                if (record.sig != sig)
                {
                    float t = record.expireAt - GenTicks.TicksGame;
                    if (t > 0.0f)
                    {
                        record.count += (short)num;
                        record.direction.x += (cell.x - center.x) * num;
                        record.direction.y += (cell.z - center.z) * num;
                    }
                    else if (t >= -updateInterval)
                    {
                        record.expireAt = (int)(GenTicks.TicksGame + updateInterval);

                        record.countPrev = record.count;
                        record.directionPrev = record.direction;

                        record.direction.x = (cell.x - center.x) * num;
                        record.direction.y = (cell.z - center.z) * num;
                        record.count = (short)num;
                    }
                    else
                    {
                        record.expireAt = (int)(GenTicks.TicksGame + updateInterval);

                        record.countPrev = 0;
                        record.directionPrev = Vector2.zero;

                        record.direction.x = (cell.x - center.x) * num;
                        record.direction.y = (cell.z - center.z) * num;
                        record.count = (short)num;
                    }
                    record.sig = sig;
                    sightArray[index] = record;
                }
            }
        }

        public Vector2 GetDirectionAt(IntVec3 cell) => GetDirectionAt(cellIndices.CellToIndex(cell));        
        public Vector2 GetDirectionAt(int index)
        {
            if (index >= 0 && index < mapCellNum)
            {
                SightRecord record = sightArray[index];
                if (record.expireAt - GenTicks.TicksGame >= -updateInterval)
                {
                    if (record.count >= record.countPrev)
                        return record.direction;
                    else
                        return record.directionPrev;
                }
            }
            return Vector2.zero;
        }

        public Vector2 GetDirectionAt(IntVec3 cell, out float enemies) => GetDirectionAt(cellIndices.CellToIndex(cell), out enemies);
        public Vector2 GetDirectionAt(int index, out float enemies)
        {
            if (index >= 0 && index < mapCellNum)
            {
                SightRecord record = sightArray[index];
                if (record.expireAt - GenTicks.TicksGame >= -updateInterval)
                {
                    if (record.count >= record.countPrev)
                    {
                        enemies = record.count;
                        return record.direction;
                    }
                    else
                    {
                        enemies = record.countPrev;
                        return record.directionPrev;
                    }
                }
            }
            enemies = 0;
            return Vector2.zero;
        }

        public bool HasCover(int index) => HasCover(cellIndices.IndexToCell(index));
        public bool HasCover(IntVec3 cell)
        {
            if (cell.InBounds(map))
            {
                SightRecord record = sightArray[cellIndices.CellToIndex(cell)];
                if (record.expireAt - GenTicks.TicksGame >= -updateInterval)
                {
                    Vector2 direction = record.direction.normalized * -1f;
                    IntVec3 endPos = cell + new Vector3(direction.x * 5, 0, direction.y * 5).ToIntVec3();
                    bool result = false;
                    GenSight.PointsOnLineOfSight(cell, endPos, (cell) =>
                    {
                        if (!result && cell.InBounds(map))
                        {
                            Thing cover = cell.GetCover(map);                            
                            if (cover != null && cover.def.Fillage == FillCategory.Partial && cover.def.category != ThingCategory.Plant)
                                result = true;
                        }
                    });
                    return result;
                }
            }
            return false;
        }

        public float GetCellSightCoverRating(int index) => GetCellSightCoverRatingInternel(cellIndices.IndexToCell(index), out _);
        public float GetCellSightCoverRating(IntVec3 cell) => GetCellSightCoverRatingInternel(cell, out _);

        public float GetCellSightCoverRating(int index, out bool hasCover) => GetCellSightCoverRatingInternel(cellIndices.IndexToCell(index), out hasCover);
        public float GetCellSightCoverRating(IntVec3 cell, out bool hasCover) => GetCellSightCoverRatingInternel(cell, out hasCover);

        // TODO remerge this.
        private float GetCellSightCoverRatingInternel(IntVec3 cell, out bool hasCover)
        {
            if (!cell.InBounds(map))
            {
                hasCover = false;
                return 0f;
            }
            int i = cellIndices.CellToIndex(cell);
            SightCoverRecord cache = coverArray[i];
            if (cache.value - GenTicks.TicksGame >= 0)
            {
                hasCover = cache.hasCover;                
                return cache.value;
            }
            cache.expireAt = GenTicks.TicksGame + 30;
            //
            hasCover = false;
            // ---------------------
            float enemies, val = 0f;
            Vector2 direction = GetDirectionAt(cell, out enemies);
            if (enemies > 0)
            {
                // Mathf.Log(64 - Mathf.Min(grid.GetDirectionAt(cell).magnitude / (0.5f * enemies), 64)) * enemies / 2f, 2f);
                // Or
                // Log2({64 - Min[64, direction.Magnitude / (0.25f * enemies)]} * enemies / 2f)
                //
                // This is a very fast aproximation of the above equation.
                int magSqr = (int)(direction.sqrMagnitude / (0.25f * enemies * enemies) / 2f);
                if (magSqr > sqrtLookup.Length)
                {
                    cache.hasCover = hasCover = false;
                    cache.value = 0f;
                    coverArray[i] = cache;
                    return 0f;
                }
                val = (64f - sqrtLookup[magSqr]) * Mathf.Min(enemies, 15) / 4f;
                val = log2Lookup[(int)(val * 10)];
                if (hasCover = HasCover(cell))
                    val *= 0.667f;               
            }            
            cache.hasCover = hasCover;
            cache.value = val;
            coverArray[i] = cache;            
            return val;
        }

        public void Next(IntVec3 center)
        {
            sig += 1;
            this.center = center;
        }

        private static float[] log2Lookup = new float[3420];
        private static float[] sqrtLookup = new float[8198];

        static SightGrid()
        {
            for (int i = 1; i < log2Lookup.Length; i++)
                log2Lookup[i] = Mathf.Log(i / 10f, 2);
            for (int i = 1; i < sqrtLookup.Length; i++)
                sqrtLookup[i] = Mathf.Sqrt(i / 2f);
        }
    }
}

