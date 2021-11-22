using System;
using RimWorld;
using Verse;
using UnityEngine;
using RimWorld.BaseGen;
using System.Linq;
using System.Collections.Generic;

namespace CombatExtended
{
    /// <summary>
    /// TL;DR Cast shadows.
    /// 
    /// The algorithem used is a modified version of   
    /// https://www.albertford.com/shadowcasting/
    /// </summary>
    public static partial class ShadowCastingUtility
    {
        private const int VISIBILITY_CARRY_MAX = 5;

        private static int carryLimit = VISIBILITY_CARRY_MAX;

        private static readonly Func<IntVec3, IntVec3>[] _transformationFuncs;        
        private static readonly Func<IntVec3, IntVec3>[] _transformationInverseFuncs;
        private static readonly Func<Vector3, Vector3>[] _transformationFuncsV3;
        private static readonly Func<Vector3, Vector3>[] _transformationInverseFuncsV3;

        private static readonly Vector3 InvalidOffset = new Vector3(0, -1, 0);
        private static int cellsScanned;

        private static IntVec3 source;
        private static List<VisibleRow> rowQueue = new List<VisibleRow>();
        private static Map map;        
        private static Action<IntVec3, int> setAction;

        static ShadowCastingUtility()
        {
            _transformationFuncs = new Func<IntVec3, IntVec3>[4];            
            _transformationFuncs[0] = (cell) =>
            {
                return cell;
            };
            _transformationFuncs[1] = (cell) =>
            {
                return new IntVec3(cell.z, 0, -1 * cell.x);
            };
            _transformationFuncs[2] = (cell) =>
            {
                return new IntVec3(-1 * cell.x, 0, -1 * cell.z);
            };
            _transformationFuncs[3] = (cell) =>
            {
                return new IntVec3(-1 * cell.z, 0, cell.x);
            };           
            _transformationInverseFuncs = new Func<IntVec3, IntVec3>[4];
            _transformationInverseFuncs[0] = (cell) =>
            {
                return cell;
            };
            _transformationInverseFuncs[1] = (cell) =>
            {
                return new IntVec3(-1 * cell.z, 0, cell.x);
            };
            _transformationInverseFuncs[2] = (cell) =>
            {
                return new IntVec3(-1 * cell.x, 0, -1 * cell.z);
            };
            _transformationInverseFuncs[3] = (cell) =>
            {
                return new IntVec3(cell.z, 0, -1 * cell.x);
            };
            _transformationFuncsV3 = new Func<Vector3, Vector3>[4];
            _transformationFuncsV3[0] = (cell) =>
            {
                return cell;
            };
            _transformationFuncsV3[1] = (cell) =>
            {
                return new Vector3(cell.z, 0, -1 * cell.x);
            };
            _transformationFuncsV3[2] = (cell) =>
            {
                return new Vector3(-1 * cell.x, 0, -1 * cell.z);
            };
            _transformationFuncsV3[3] = (cell) =>
            {
                return new Vector3(-1 * cell.z, 0, cell.x);
            };
            _transformationInverseFuncsV3 = new Func<Vector3, Vector3>[4];
            _transformationInverseFuncsV3[0] = (cell) =>
            {
                return cell;
            };
            _transformationInverseFuncsV3[1] = (cell) =>
            {
                return new Vector3(-1 * cell.z, 0, cell.x);
            };
            _transformationInverseFuncsV3[2] = (cell) =>
            {
                return new Vector3(-1 * cell.x, 0, -1 * cell.z);
            };
            _transformationInverseFuncsV3[3] = (cell) =>
            {
                return new Vector3(cell.z, 0, -1 * cell.x);
            };
        }

        private struct VisibleRow
        {
            public float startSlope;
            public float endSlope;

            public int visibilityCarry;            
            public int depth;
            public int quartor;
            public int maxDepth;            

            public IEnumerable<Vector3> Tiles()
            {
                int min = (int)Mathf.Floor(this.startSlope * this.depth + 0.5f);
                int max = (int)Mathf.Ceil(this.endSlope * this.depth - 0.5f);
                
                for (int i = min; i <= max; i++)
                {                    
                    yield return new Vector3(depth, 0, i);
                }
            }

            public IntVec3 Transform(IntVec3 oldOffset)
            {
                return _transformationFuncs[quartor](oldOffset);
            }

            public VisibleRow Next()
            {
                VisibleRow row = new VisibleRow();
                row.endSlope = this.endSlope;
                row.startSlope = this.startSlope;
                row.depth = this.depth + 1;
                row.maxDepth = maxDepth;
                row.quartor = this.quartor;
                row.visibilityCarry = visibilityCarry;
                return row;
            }

            public static VisibleRow First
            {
                get
                {
                    VisibleRow row = new VisibleRow();
                    row.startSlope = -1;
                    row.endSlope = 1;
                    row.depth = 1;
                    row.visibilityCarry = 1;
                    return row;
                }
            }
        }       

        private static bool IsValid(Vector3 point) => point.y >= 0;
        private static float GetSlope(Vector3 point) => (2f * point.z - 1.0f) / (2f * point.x);

        private static int GetQurator(IntVec3 cell)
        {
            int x = cell.x;
            int z = cell.z;
            if (x > 0 && Math.Abs(z) < x)
            {
                return 0;
            }
            else if (x < 0 && Math.Abs(z) < Math.Abs(x))
            {
                return 2;
            }
            else if (z > 0 && Math.Abs(x) <= z)
            {
                return 3;
            }
            return 1;
        }

        private static void TryWeightedScan(VisibleRow start)
        {            
            rowQueue.Clear();
            rowQueue.Add(start);
            
            while (rowQueue.Count > 0)
            {                
                VisibleRow nextRow;                
                VisibleRow row = rowQueue.Pop();
                                
                if (row.depth > row.maxDepth || row.visibilityCarry <= 1e-5f)
                {
                    continue;
                }
                var lastCell = InvalidOffset;                
                var lastIsWall = false;
                var lastIsCover = false;

                foreach (Vector3 offset in row.Tiles())
                {                                        
                    var cell = source + row.Transform(offset.ToIntVec3());
                    var isWall = !cell.InBounds(map) || !cell.CanBeSeenOver(map);
                    var isCover = !isWall && cell.GetCover(map)?.def.Fillage == FillCategory.Partial;                    
                    
                    if (isWall || (offset.z >= row.depth * row.startSlope && offset.z <= row.depth * row.endSlope))
                    {                        
                        setAction(cell, row.visibilityCarry);
                    }
                    if(isCover && row.visibilityCarry >= carryLimit)
                    {
                        isCover = false;
                        isWall = true;
                    }
                    if (IsValid(lastCell)) // check so it's a valid offsets
                    {
                        if (lastIsWall == isWall)
                        {
                            if (isCover != lastIsCover && lastIsWall == isWall) // first handle cover 
                            {
                                nextRow = row.Next();
                                nextRow.endSlope = GetSlope(offset);
                                if (lastIsCover)
                                {
                                    nextRow.visibilityCarry += 1;
                                }
                                rowQueue.Add(nextRow);
                                row.startSlope = GetSlope(offset);
                            }
                        }
                        else if (!isWall && lastIsWall)
                        {
                            row.startSlope = GetSlope(offset);
                        }
                        else if (isWall && !lastIsWall)
                        {
                            nextRow = row.Next();
                            nextRow.endSlope = GetSlope(offset);
                            if (lastIsCover)
                            {
                                nextRow.visibilityCarry += 1;
                            }
                            rowQueue.Add(nextRow);
                        }                        
                    }                    
                    cellsScanned++;                    
                    lastCell = offset;                                      
                    lastIsWall = isWall;
                    lastIsCover = isCover;
                }                
                if (lastCell.y >= 0 && !lastIsWall)
                {
                    nextRow = row.Next();
                    if (lastIsCover)
                    {
                        nextRow.visibilityCarry += 1;
                    }
                    rowQueue.Add(nextRow);                    
                }
            }
        }

        private static void TryVisibilityScan(VisibleRow start)
        {
            rowQueue.Clear();
            rowQueue.Add(start);
            while (rowQueue.Count > 0)
            {
                VisibleRow nextRow;
                VisibleRow row = rowQueue.Pop();
                
                if (row.depth > row.maxDepth)
                {
                    continue;
                }
                var lastCell = InvalidOffset;
                var lastIsWall = false;
                
                foreach (Vector3 offset in row.Tiles())
                {
                    var cell = source + row.Transform(offset.ToIntVec3());
                    var isWall = !cell.InBounds(map) || !cell.CanBeSeenOverFast(map);                    

                    if (isWall || (offset.z >= row.depth * row.startSlope && offset.z <= row.depth * row.endSlope))
                    {
                        setAction(cell, 1);
                    }
                    if (IsValid(lastCell)) // check so it's a valid offsets
                    {                        
                        if (!isWall && lastIsWall)
                        {
                            row.startSlope = GetSlope(offset);
                        }
                        else if (isWall && !lastIsWall)
                        {
                            nextRow = row.Next();
                            nextRow.endSlope = GetSlope(offset);                               
                            rowQueue.Add(nextRow);
                        }                        
                    }                    
                    cellsScanned++;                    
                    lastCell = offset;
                    lastIsWall = isWall;
                }
                if (lastCell.y >= 0 && !lastIsWall)
                {                                    
                    rowQueue.Add(row.Next());
                }
            }
        }

        private static void TryCastVisibility(float startSlope, float endSlope, int quartor, int maxDepth) => TryCast(TryVisibilityScan, startSlope, endSlope, quartor, maxDepth);
        private static void TryCastWeighted(float startSlope, float endSlope, int quartor, int maxDepth) => TryCast(TryWeightedScan, startSlope, endSlope, quartor, maxDepth);

        private static void TryCast(Action<VisibleRow> castAction, float startSlope, float endSlope, int quartor, int maxDepth)
        {            
            if (endSlope > 1.0f || startSlope < -1 || startSlope > endSlope)
            {
                throw new InvalidOperationException($"CE: Scan quartor {quartor} endSlope and start slop must be between (-1, 1) but got start:{startSlope}\tend:{endSlope}");
            }
            VisibleRow arc = VisibleRow.First;
            arc.startSlope = startSlope;
            arc.endSlope = endSlope;
            arc.visibilityCarry = 1;
            arc.maxDepth = maxDepth;
            arc.quartor = quartor;
            castAction(arc);
        }                         
    }
}

