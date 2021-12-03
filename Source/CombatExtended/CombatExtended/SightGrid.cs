using System;
using RimWorld;
using Verse;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Steamworks;
using System.Text;

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
            public float visibility;
            public float visibilityPrev;
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
        
        private float range;
        private IntVec3 center;
        private readonly float updateInterval;
        private readonly int updateIntervalInt;
        private short sig = 13;
        private CellIndices cellIndices;
        private SightRecord[] sightArray;
        private SightCoverRecord[] coverArray;
        private Faction faction;
        private Map map;
        private int mapSizeX;
        private int mapSizeZ;
        private int mapCellNum;

        private int Cycle
        {
            get => GenTicks.TicksGame / updateIntervalInt;
        }

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
            this.updateIntervalInt = (int)updateInterval;
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
                    if (record.expireAt - Cycle > 0)                        
                        return Math.Max(record.count, record.countPrev);
                    else if(record.expireAt - Cycle == 0)
                        return record.count;
                }
                return 0;
            }
        }

        public void Set(IntVec3 cell, int num, int dist) => Set(cellIndices.CellToIndex(cell), num, dist);
        public void Set(int index, int num, int dist)
        {
            if (index >= 0 && index < mapCellNum)
            {
                SightRecord record = sightArray[index];                
                if (record.sig != sig)
                {
                    IntVec3 cell = cellIndices.IndexToCell(index);

                    float t = record.expireAt - Cycle;                    
                    if (t > 0)
                    {
                        record.count += (short)num;
                        float visibility = (range - dist) / range * num;
                        record.visibility += visibility;
                        record.direction.x += (cell.x - center.x) * visibility;
                        record.direction.y += (cell.z - center.z) * visibility;                        
                    }
                    else if (t == 0)
                    {
                        record.expireAt = Cycle + 1;

                        record.countPrev = record.count;
                        record.directionPrev = record.direction;
                        record.visibilityPrev = record.visibility;

                        record.visibility = (range - dist) / range * num;
                        record.direction.x = (cell.x - center.x) * record.visibility;
                        record.direction.y = (cell.z - center.z) * record.visibility;
                        record.count = (short)num;
                    }
                    else
                    {
                        record.expireAt = Cycle + 1;

                        record.countPrev = 0;
                        record.directionPrev = Vector2.zero;
                        record.visibilityPrev = 0;

                        record.visibility = (range - dist) / range * num;
                        record.direction.x = (cell.x - center.x) * record.visibility;
                        record.direction.y = (cell.z - center.z) * record.visibility;
                        record.count = (short)num;
                    }
                    record.sig = sig;
                    sightArray[index] = record;
                }
            }
        }       

        public float GetVisibility(int index) => GetVisibility(index, out _);
        public float GetVisibility(IntVec3 cell) => GetVisibility(cellIndices.CellToIndex(cell), out _);

        public float GetVisibility(IntVec3 cell, out int enemies) => GetVisibility(cellIndices.CellToIndex(cell), out enemies);
        public float GetVisibility(int index, out int enemies)
        {
            if (index >= 0 && index < mapCellNum)
            {
                SightRecord record = sightArray[index];
                if (record.expireAt - Cycle > 0)
                {                   
                    enemies = record.countPrev;
                    return Mathf.Max(record.visibilityPrev, record.visibility);                   
                }
                else if(record.expireAt - Cycle == 0)
                {
                    enemies = record.countPrev;
                    return record.visibility;
                }
            }
            enemies = 0;
            return 0f;
        }

        public Vector2 GetDirectionAt(IntVec3 cell) => GetDirectionAt(cellIndices.CellToIndex(cell));
        public Vector2 GetDirectionAt(int index)
        {
            if (index >= 0 && index < mapCellNum)
            {
                SightRecord record = sightArray[index];
                if (record.expireAt - Cycle > 0)
                {
                    if (record.count > record.countPrev)
                        return record.direction;
                    else
                        return record.directionPrev;
                }
                else if (record.expireAt - Cycle == 0)
                    return record.direction;

            }
            return Vector2.zero;
        }

        public Vector2 GetDirectionAt(IntVec3 cell, out float enemies) => GetDirectionAt(cellIndices.CellToIndex(cell), out enemies);
        public Vector2 GetDirectionAt(int index, out float enemies)
        {
            if (index >= 0 && index < mapCellNum)
            {
                SightRecord record = sightArray[index];
                if (record.expireAt - Cycle > 0)
                {
                    if (record.count > record.countPrev)
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
                else if(record.expireAt - Cycle == 0)
                {
                    enemies = record.count;
                    return record.direction;
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
                if (record.expireAt - Cycle >= 0)
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
            hasCover = HasCover(cell);
            if(hasCover)
                return GetVisibility(cell) * 0.5f;
            else
                return GetVisibility(cell);
        }

        public void Next(IntVec3 center, float range)
        {
            sig += 1;
            this.center = center;
            this.range = range * 2.0f;
        }

        private static StringBuilder _builder = new StringBuilder();

        public string GetDebugInfoAt(IntVec3 cell) => GetDebugInfoAt(map.cellIndices.CellToIndex(cell));
        public string GetDebugInfoAt(int index)
        {
            if (index >= 0 && index < mapCellNum)
            {
                SightRecord record = sightArray[index];
                _builder.Clear();
                _builder.AppendFormat("<color=grey>{0}</color> {1}\n", "Partially expired ", record.expireAt - Cycle == 0);
                _builder.AppendFormat("<color=grey>{0}</color> {1}", "Expired           ", record.expireAt - Cycle < 0);
                _builder.AppendLine();
                _builder.AppendFormat("<color=orange>{0}</color> {1}\n" +
                    "<color=grey>cur</color>  {2}\t" +
                    "<color=grey>prev</color> {3}", "Enemies", this[index], record.count, record.countPrev);
                _builder.AppendLine();
                _builder.AppendFormat("<color=orange>{0}</color> {1}\n " +
                    "<color=grey>cur</color>  {2}\t" +
                    "<color=grey>prev</color> {3}", "Visibility", GetVisibility(index), Math.Round(record.visibility, 2), Math.Round(record.visibilityPrev, 2));
                _builder.AppendLine();
                _builder.AppendFormat("<color=orange>{0}</color> {1}\n" +
                    "<color=grey>cur</color>  {2} " +
                    "<color=grey>prev</color> {3}", "Direction", GetDirectionAt(index), record.direction, record.direction);
                return _builder.ToString();
            }
            return "<color=red>Out of bounds</color>";
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

