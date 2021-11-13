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

        public static void CastWeighted(this Map map, IntVec3 source, int maxRadius, bool reset = true) => Cast(map, TryCastWeighted, source, maxRadius, reset);
        public static void CastVisibility(this Map map, IntVec3 source, int maxRadius, bool reset = true) => Cast(map, TryCastVisibility, source, maxRadius, reset);

        private static void Cast(Map map, Action<float, float, int, int> castingAction, IntVec3 source, int maxRadius, bool reset)
        {
            ShadowCastingUtility.map = map;
            ShadowCastingUtility.grid = map.GetComponent<ShadowGrid>();
            if (reset)
            {
                ShadowCastingUtility.grid.Reset();
            }
            ShadowCastingUtility.source = source;
            int maxDepth = (int)(maxRadius * 1.43f);
            for (int i = 0; i < 4; i++)
            {
                castingAction(-1, 1, i, maxDepth);
            }
            ShadowCastingUtility.map = null;
            ShadowCastingUtility.grid = null;
            ShadowCastingUtility.source = IntVec3.Invalid;
        }

        public static void CastWeighted(this Map map, IntVec3 source, IntVec3 target, float baseWidth = 5, bool reset = true) => Cast(map, TryCastWeighted, source, target, baseWidth, reset);
        public static void CastVisibility(this Map map, IntVec3 source, IntVec3 target, float baseWidth = 5, bool reset = true) => Cast(map, TryCastVisibility, source, target, baseWidth, reset);        

        private static void Cast(Map map, Action<float, float, int, int> castingAction, IntVec3 source, IntVec3 target, float baseWidth, bool reset)
        {
            if (target.DistanceTo(source) < 2)
            {
                return;
            }
            ShadowCastingUtility.map = map;
            ShadowCastingUtility.grid = map.GetComponent<ShadowGrid>();
            ShadowCastingUtility.source = source;
            if (reset)
            {
                ShadowCastingUtility.grid.Reset();
            }            
            int maxDepth = (int) (source.DistanceTo(target) * 1.45f);
            int quartor = GetQurator(target - source);            
            Vector3 relTarget = _transformationInverseFuncsV3[quartor]((target - source).ToVector3());
            Vector3 relStart = relTarget + new Vector3(0, 0, -baseWidth / 2f);
            Vector3 relEnd = relTarget + new Vector3(0, 0, baseWidth / 2f);
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
            ShadowCastingUtility.grid = null;
            ShadowCastingUtility.source = IntVec3.Invalid;
        }
    }
}

