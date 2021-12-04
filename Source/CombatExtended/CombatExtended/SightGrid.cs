using System;
using RimWorld;
using Verse;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Steamworks;
using System.Text;
using System.Runtime.CompilerServices;

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
            /// <summary>
            /// At what cycle does this record expire.
            /// </summary>
            public int expireAt;
            /// <summary>
            /// Used to prevent set a record twice in a single operation.
            /// </summary>
            public short sig;
            /// <summary>
            /// The number of overlaping casts.
            /// </summary>
            public short count;            
            /// <summary>
            /// The previous number of overlaping casts.
            /// </summary>
            public short countPrev;
            /// <summary>
            /// Indicates how much this cell is visible/close to casters.
            /// </summary>
            public float visibility;
            /// <summary>
            /// The previous visibility value.
            /// </summary>
            public float visibilityPrev;
            /// <summary>
            /// The general direction of casters.
            /// </summary>
            public Vector2 direction;
            /// <summary>
            /// The previous general direction of casters;
            /// </summary>
            public Vector2 directionPrev;
            /// <summary>
            /// A bit map that is used to indicate an pool of potential casters.
            /// </summary>
            public UInt64 casterFlags;
            /// <summary>
            /// The previous caster flags.
            /// </summary>
            public UInt64 casterFlagsPrev;
            /// <summary>
            /// Will prepare this record for the next cycle by either reseting prev fields or replacing them with the current values.
            /// </summary>            
            /// <param name="reset">Wether to reset prev</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Next(bool expired)
            {
                if (!expired)
                {                    
                    directionPrev   = direction;
                    countPrev       = count;
                    casterFlagsPrev = casterFlags;
                    visibilityPrev = visibility;
                    casterFlagsPrev = casterFlags;
                }
                else
                {
                    directionPrev = Vector2.zero; ;
                    countPrev       = 0;
                    casterFlagsPrev = 0;
                    visibilityPrev  = 0;
                    casterFlagsPrev = 0;
                }
            }                       
        }

        //
        // State fields.
        #region Fields

        /// <summary>
        /// How far can the current caster see.
        /// </summary>
        private float range;
        /// <summary>
        /// The center of the the current casting operation.
        /// </summary>
        private IntVec3 center;                        
        /// <summary>
        /// The signature of the current operation.
        /// </summary>
        private short sig = 13;
        /// <summary>
        /// The flags of the current caster.
        /// </summary>
        private UInt64 currentCasterFlags;
        /// <summary>
        /// Ticks between cycles.
        /// </summary>
        private int cycle = 19;

        #endregion

        //
        // Read only fields.
        #region ReadonlyFields

        /// <summary>
        /// CellIndices of the parent map.
        /// </summary>
        private readonly CellIndices cellIndices;
        /// <summary>
        /// The sight array with the size of the parent map.
        /// </summary>
        private readonly SightRecord[] sightArray;        
        /// <summary>
        /// Parent map.
        /// </summary>
        private readonly Map map;
        /// <summary>
        /// Number of cells in parent map.
        /// </summary>
        private readonly int mapCellNum;

        #endregion

        /// <summary>
        /// The current cycle of update.
        /// </summary>
        public int CycleNum
        {
            get => cycle;
        }

        public SightGrid(Map map)
        {
            cellIndices = map.cellIndices;                        
            mapCellNum = cellIndices.NumGridCells;            
            sightArray = new SightRecord[map.cellIndices.NumGridCells];            
            this.map = map;            
            for (int i = 0; i < sightArray.Length; i++)
            {               
                sightArray[i] = new SightRecord()
                {
                    sig = -1,
                    expireAt = -1,
                    direction = Vector3.zero
                };
            }
        }

        /// <summary>
        /// Return the number of enemies in cell.
        /// </summary>
        /// <param name="cell">Cell</param>
        /// <returns>Number of enemies in cell.</returns>
        public float this[IntVec3 cell]
        {
            get => this[cellIndices.CellToIndex(cell)];            
        }

        /// <summary>
        /// Return the number of enemies in cell index.
        /// </summary>
        /// <param name="cell">Cell</param>
        /// <returns>Number of enemies in cell index.</returns>
        public float this[int index]
        {
            get
            {
                if (index >= 0 && index < mapCellNum)
                {
                    SightRecord record = sightArray[index];
                    if (record.expireAt - CycleNum > 0)                        
                        return Math.Max(record.count, record.countPrev);
                    else if(record.expireAt - CycleNum == 0)
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
                    float t = record.expireAt - CycleNum;
                    if (t > 0)
                    {
                        record.count += (short)num;
                        float visibility = (range - dist) / range * num;

                        record.visibility += visibility;
                        record.direction.x += (cell.x - center.x) * visibility;
                        record.direction.y += (cell.z - center.z) * visibility;
                        record.casterFlags |= currentCasterFlags;

                        if(record.count == 4)
                        {
                            Log.Message($"{cycle} {num}");
                        }
                    }
                    else
                    {
                        if (t == 0)
                        {
                            record.expireAt = CycleNum + 1;
                            record.Next(expired: false);                            
                        }
                        else
                        {
                            record.expireAt = CycleNum + 1;
                            record.Next(expired: true);                            
                        }
                        record.count = (short)num;
                        record.visibility = (range - dist) / range * num;
                        record.direction.x = (cell.x - center.x) * record.visibility;
                        record.direction.y = (cell.z - center.z) * record.visibility;                        
                        record.casterFlags = currentCasterFlags;
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
                if (record.expireAt - CycleNum > 0)
                {                   
                    enemies = record.countPrev;
                    return Mathf.Max((record.visibilityPrev + record.countPrev) / 2f, (record.visibility + record.count) / 2f, 0f);                   
                }
                else if(record.expireAt - CycleNum == 0)
                {
                    enemies = record.countPrev;
                    return Mathf.Max((record.visibility + record.count) / 2f, 0f);
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
                if (record.expireAt - CycleNum > 0)
                {
                    if (record.count > record.countPrev)
                        return record.direction;
                    else
                        return record.directionPrev;
                }
                else if (record.expireAt - CycleNum == 0)
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
                if (record.expireAt - CycleNum > 0)
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
                else if(record.expireAt - CycleNum == 0)
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
                if (record.expireAt - CycleNum >= 0)
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

        /// <summary>
        /// Prepare the grid for a new casting operation.
        /// </summary>
        /// <param name="center">Center of casting.</param>
        /// <param name="range">Expected range of casting.</param>
        /// <param name="casterFlags">caster's Flags</param>
        public void Next(IntVec3 center, float range, UInt64 casterFlags = 0)
        {
            sig++;
            this.center = center;
            this.range = range * 1.3f;
            this.currentCasterFlags = 0;
        }

        public void NextCycle()
        {
            sig++;
            cycle++;
            this.center = IntVec3.Invalid;
            this.range = 0;
            this.currentCasterFlags = 0;
        }

        private static StringBuilder _builder = new StringBuilder();

        public string GetDebugInfoAt(IntVec3 cell) => GetDebugInfoAt(map.cellIndices.CellToIndex(cell));
        public string GetDebugInfoAt(int index)
        {
            if (index >= 0 && index < mapCellNum)
            {
                SightRecord record = sightArray[index];
                _builder.Clear();
                _builder.AppendFormat("<color=grey>{0}</color> {1}\n", "Partially expired ", record.expireAt - CycleNum == 0);
                _builder.AppendFormat("<color=grey>{0}</color> {1}", "Expired           ", record.expireAt - CycleNum < 0);
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

