using System;
using RimWorld;
using Verse;
using UnityEngine;
using RimWorld.BaseGen;
using System.Linq;
using System.Collections.Generic;

namespace CombatExtended
{
	public static partial class ShadowCastingUtility
	{
        private static readonly IntVec3 _offsetH = new IntVec3(1, 0, 0);
        private static readonly IntVec3 _offsetV = new IntVec3(0, 0, 1);

        private static IntVec3 GetBaseOffset(int quartor) => (quartor == 0 || quartor == 2) ? _offsetV : _offsetH;

        private static int GetNextQuartor(int quartor) => (quartor + 1) % 4;
        private static int GetPreviousQuartor(int quartor) => (quartor - 1) < 0 ? 3 : (quartor - 1);

        /// <summary>
        /// Evaluate all cells around the source for visibility (float value).
        /// Note: this is a slower but more accurate version of CastVisibility.
        /// </summary>
        /// <param name="map">Map</param>
        /// <param name="source">Source cell</param>
        /// <param name="radius">Radius</param>        
        public static void CastWeighted(Map map, IntVec3 source, Action<IntVec3, int, int> setAction, int radius) => Cast(map, TryCastWeighted, setAction, source, radius);
        /// <summary>
        /// Evaluate all cells around the source for visibility (float value).
        /// Note: this is a slower but more accurate version of CastVisibility.
        /// </summary>
        /// <param name="map">Map</param>
        /// <param name="source">Source cell</param>
        /// <param name="maxRadius">Radius</param>
        /// <param name="cellCount">number of cells sat (the action called in)</param>
        public static void CastWeighted(Map map, IntVec3 source, Action<IntVec3, int, int> action, int radius, int carryLimit, out int cellCount)
        {
            ShadowCastingUtility.carryLimit = carryLimit;
            CastWeighted(map, source, action, radius);
            cellCount = cellsScanned;
            ShadowCastingUtility.carryLimit = VISIBILITY_CARRY_MAX;
        }

        /// <summary>
        /// Evaluate all cells around the source for visibility (on/off).
        /// Note: this is a faster but less accurate version of CastVisibility.
        /// </summary>
        /// <param name="map">Map</param>
        /// <param name="source">Source cell</param>
        /// <param name="radius">Radius</param>        
        public static void CastVisibility(Map map, IntVec3 source, Action<IntVec3, int> action, int radius) => Cast(map, TryCastVisibility, (cell, _, dist) => action(cell, dist), source, radius);
        /// <summary>
        /// Evaluate all cells around the source for visibility (on/off).
        /// Note: this is a faster but less accurate version of CastVisibility.
        /// </summary>
        /// <param name="map">Map</param>
        /// <param name="source">Source cell</param>
        /// <param name="radius">Radius</param>        
        /// <param name="cellCount">number of cells sat</param>
        public static void CastVisibility(Map map, IntVec3 source, Action<IntVec3, int> action, int radius, out int cellCount)
        {
            CastVisibility(map, source, action, radius);
            cellCount = ShadowCastingUtility.cellsScanned;
        }       

        private static void Cast(Map map, Action<float, float, int, int> castingAction, Action<IntVec3, int, int> action, IntVec3 source, int radius)
        {
            ShadowCastingUtility.map = map;
            ShadowCastingUtility.source = source;
            ShadowCastingUtility.setAction = action;
            ShadowCastingUtility.cellsScanned = 0;
            int maxDepth = (int)(radius * 1.43f);
            for (int i = 0; i < 4; i++)
            {
                castingAction(-1, 1, i, maxDepth);
            }
            ShadowCastingUtility.map = null;
            ShadowCastingUtility.source = IntVec3.Invalid;
        }

        /// <summary>
        /// Evaluate visible cells from the source in the direction of target. Will result in storing the cover visiblity (on/off) for each each cell in the ShadowGrid
        /// </summary>
        /// <param name="map">Map</param>
        /// <param name="source">Source cell</param>
        /// <param name="direction">Direction</param>
        /// <param name="baseWidth">What is the maximum amount of cells (width) to be scanned</param>        
        public static void CastVisibility(Map map, IntVec3 source, Vector3 direction, Action<IntVec3, int> action, float radius, float baseWidth) => Cast(map, TryCastVisibility, (cell, _, dist) => action(cell, dist), source, (source.ToVector3() + direction.normalized * radius).ToIntVec3(), baseWidth);
        /// <summary>
        /// Evaluate visible cells from the source in the direction of target. Will result in storing the cover visiblity (on/off) for each each cell in the ShadowGrid
        /// </summary>
        /// <param name="map">Map</param>
        /// <param name="source">Source cell</param>
        /// <param name="direction">Direction</param>
        /// <param name="baseWidth">What is the maximum amount of cells (width) to be scanned</param>
        /// <param name="cellCount">number of cells sat (the action called in)</param>
        public static void CastVisibility(Map map, IntVec3 source, Vector3 direction, Action<IntVec3, int> action, float radius, float baseWidth, out int cellCount)
        {
            CastVisibility(map, source, direction, action, radius, baseWidth);
            cellCount = ShadowCastingUtility.cellsScanned;
        }

        /// <summary>
        /// Evaluate visible cells from the source in the direction of target. Will result in storing the cover visiblity (float value) scoring for each each cell in the ShadowGrid
        /// </summary>
        /// <param name="map">Map</param>
        /// <param name="source">Source cell</param>
        /// <param name="direction">Direction</param>
        /// <param name="action">Set action (x, z, current_ray_value)</param> 
        /// <param name="baseWidth">What is the maximum amount of cells (width) to be scanned</param>          
        public static void CastWeighted(Map map, IntVec3 source, Vector3 direction, Action<IntVec3, int, int> action, float radius, float baseWidth) => Cast(map, TryCastWeighted, action, source, (source.ToVector3() + direction.normalized * radius).ToIntVec3(), baseWidth);
        /// <summary>
        /// Evaluate visible cells from the source in the direction of target. Will result in storing the cover visiblity (float value) scoring for each each cell in the ShadowGrid
        /// </summary>
        /// <param name="map">Map</param>
        /// <param name="source">Source cell</param>
        /// <param name="direction">Direction</param>
        /// <param name="action">Set action (x, z, current_ray_value)</param>
        /// <param name="baseWidth">What is the maximum amount of cells (width) to be scanned</param>
        /// <param name="cellCount">number of cells sat (the action called in)</param>
        /// <param name="carryLimit">How many layers of cover before it's no longer visible</param>  
        public static void CastWeighted(Map map, IntVec3 source, Vector3 direction, Action<IntVec3, int, int> action, float radius, float baseWidth, int carryLimit, out int cellCount)
        {
            ShadowCastingUtility.carryLimit = carryLimit;
            CastWeighted(map, source, direction, action, radius, baseWidth);            
            cellCount = cellsScanned;
            ShadowCastingUtility.carryLimit = VISIBILITY_CARRY_MAX;
        }          

        private static void Cast(Map map, Action<float, float, int, int> castingAction, Action<IntVec3, int, int> setAction, IntVec3 source, IntVec3 target, float baseWidth)
        {
            if (target.DistanceTo(source) < 2)
            {
                return;
            }            
            ShadowCastingUtility.map = map;            
            ShadowCastingUtility.source = source;
            ShadowCastingUtility.setAction = setAction;
            ShadowCastingUtility.cellsScanned = 0;
            //
            // get which quartor the target is in.
            int quartor = GetQurator(target - source);            
            Vector3 relTarget = _transformationInverseFuncsV3[quartor]((target - source).ToVector3());
            Vector3 relStart = relTarget + new Vector3(0, 0, -baseWidth / 2f);
            Vector3 relEnd = relTarget + new Vector3(0, 0, baseWidth / 2f);
            int maxDepth = (int)(source.DistanceTo(target));
            float startSlope = GetSlope(relStart);
            float endSlope = GetSlope(relEnd);                       
            if(startSlope < -1)
            {                                  
                float slope = Mathf.Max(startSlope + 2, 0);
                castingAction(slope, 1, GetNextQuartor(quartor), maxDepth);
                startSlope = -1;
            }
            if (endSlope > 1)
            {                
                float slope = Mathf.Min(endSlope - 2, 0);                
                castingAction(-1, slope, GetPreviousQuartor(quartor), maxDepth);
                endSlope = 1;
            }
            castingAction(startSlope, endSlope, quartor, maxDepth);
            ShadowCastingUtility.map = null;            
            ShadowCastingUtility.source = IntVec3.Invalid;
        }        
    }
}

