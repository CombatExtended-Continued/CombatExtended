using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using UnityEngine;

namespace CombatExtended.HarmonyCE
{
    /* Targetting Verse.GenRadial.RadialPatternCount constant.
     * Specifically this looks for all methods which use that constant and prefix-replaces them.
     * The replacement version is about 3x faster, with near constant complexity.
     */

    internal static class Harmony_GenRadial
    {
        public const int MAX_RADIUS = 119; // if adjusting this higher than 200, enable the binary search code.
        public const int RadialPatternCount = 44469; // found by observation
        public static readonly IntVec3[] RadialPattern = new IntVec3[RadialPatternCount]; // radius -> cells
        public static readonly float[] RadialPatternRadii = new float[RadialPatternCount]; // cell -> radius
        public static readonly int[] RadialPatternNumCells = new int[MAX_RADIUS * MAX_RADIUS + 1]; // index of squared radius to last cell guaranteed inside

        /*
         * This method populates the static arrays in this class. They should match the values in GenRadial up to the 10000 entry.
         * The core algorithm cycles through all cells directly. It calculates the minimum and maximum z value, which requires a single square root per X
         * As it generates the cells, it sorts them into buckets based on the square distance.
         * Once generated, it flattens the buckets starting from the closest, building all 3 caches in the process.
         */
        static Harmony_GenRadial()
        {
            int rad_sq = MAX_RADIUS * MAX_RADIUS;
            List<IntVec3>[] buckets = new List<IntVec3>[rad_sq + 1]; // + 1 because of the 0-length center cell

            for (int b = 0; b < rad_sq + 1; b++) // List<IntVec3> is not default constructed in an array
            {
                buckets[b] = new List<IntVec3>();
            }
            for (int x = -MAX_RADIUS; x < MAX_RADIUS + 1; x++) // walk over all x in range
            {
                int mz = (int)(Mathf.Sqrt(rad_sq - x * x)); // given an x, find the min/max z required
                for (int z = -mz; z < mz + 1; z++) // iterate the z cells determined by the previous step
                {
                    int r_sq = x * x + z * z; // calculate their squared displacement
                    buckets[r_sq].Add(new IntVec3(x, 0, z)); // put them in the correct bucket
                }
            }
            int ctr = 0;
            for (int r_sq = 0; r_sq < rad_sq + 1; r_sq++) // now iterate over all the buckets and flatten them into the cache.
            {
                float r = Mathf.Sqrt(r_sq);
                List<IntVec3> bucket = buckets[r_sq];
                foreach (IntVec3 cell in bucket)
                {
                    RadialPattern[ctr] = cell;
                    RadialPatternRadii[ctr++] = r;
                }
                RadialPatternNumCells[r_sq] = ctr; // this points to the *last* cell that fits within r_sq radius.
            }
        }

        // used for debug outputs (probably not used much).
        static readonly string logPrefix = "Combat Extended :: " + typeof(Harmony_GenRadial).Name + " :: ";

        // Old fashioned reflection *must* be done before getting harmony involved, or the jit will cache the fields on us.
        internal static void Patch()
        {
            FieldInfo radialPattern = typeof(GenRadial).GetField("RadialPattern", BindingFlags.Static | BindingFlags.Public);
            radialPattern.SetValue(null, RadialPattern);

            FieldInfo patternRadii = typeof(GenRadial).GetField("RadialPatternRadii", BindingFlags.Static | BindingFlags.Public);
            patternRadii.SetValue(null, RadialPatternRadii);

            HarmonyBase.instance.Patch(typeof(GenRadial).GetMethod("NumCellsInRadius"),
                                       new HarmonyMethod(typeof(Harmony_GenRadial), nameof(Harmony_GenRadial.NumCellsInRadius)), null, null);

        }

        public static bool NumCellsInRadius(out int __result, float radius)
        {
            // Special cases
            if (radius < 2f || radius >= MAX_RADIUS)
            {
                if (radius < 0f)
                    __result = 0;
                else if (radius < 1f)
                    __result = 1;
                else if (radius < 1.414213562f)
                    __result = 5;
                else if (radius < 2f)
                    __result = 9;
                else
                {
                    __result = RadialPatternCount;
                    CheckRadius(radius);
                }
                return false;
            }

            int radiusSquaredInt = (int)(radius * radius);
            // The answer is at least (25/8 * radius * radius - 11)
            int lowerBound = ((radiusSquaredInt * 25) >> 3) - 11;
#if BINSEARCH
            // The upper bound is set to (101/32 * radius * radius)
            //   some small radius actually goes above that
            //   but the error is small (< 16) and linear search can fix it
            int upperBound = (radiusSquaredInt * 101) >> 5;
            if (upperBound > RadialPatternCount)
                upperBound = RadialPatternCount;
            // If the gap is large, perform binary search
            while (upperBound - lowerBound > 88)
            {
                int mid = (lowerBound + upperBound) >> 1;
                if (RadialPatternRadii[mid] <= radius)
                    lowerBound = mid;
                else
                    upperBound = mid;
            }
#endif
            // Linear search:
            // When (int)radius + (int)diagonal is odd,  the answer is always 8n + 5
            // When (int)radius + (int)diagonal is even, the answer is always 8n + 1
            // So we can set the last three bits of lowerBound
            // and check only every 8 numbers
            lowerBound = (lowerBound & -7) | (HasOddCells(radius) ? 5 : 1);
            while (RadialPatternRadii[lowerBound] <= radius)
                lowerBound += 8;
            __result = lowerBound;
            return false;
        }
        
        // A helper function for Prefix_NumCellsInRadius()
        // Evaluates if (int)r + (int)d is an even number (round toward zero)
        // Where d = r * sqrt(1/2)
        // When (int)radius + (int)diagonal is odd,  the answer is always 8n + 5
        // When (int)radius + (int)diagonal is even, the answer is always 8n + 1
        // Use IEEE-754 bit hacks
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasOddCells(float r)
        {
            const float SqrtHalf = 0.707106781f;
            float d = r * SqrtHalf;
            // bit hack magic
            int bitr = BitConverter.SingleToInt32Bits(r);
            int bitd = BitConverter.SingleToInt32Bits(d);
            // Slightly different from the real IEEE 754 mantissa
            //   -- we first take the fractional part
            //   -- then add back the missing starting 1
            int mantissar = bitr & 0x007F_FFFF | 0x0080_0000;
            int mantissad = bitd & 0x007F_FFFF | 0x0080_0000;
            // We want to shift the fractional part out
            //   -- shift 23, plus or minus the exponent part
            //   -- the exponenet part itself is offset by 127
            int shiftr = 23 + 127 - ((bitr >> 23) & 0xFF);
            int shiftd = 23 + 127 - ((bitd >> 23) & 0xFF);
            return (((mantissar >> shiftr) ^ (mantissad >> shiftd)) & 1) != 0;
        }
        
        // A separate method to handle when radius exceeds MAX_RADIUS
        // Apparently loading all these strings for formatting is very slow
        // Even when they are not executed, likely due to branch predictions
        // Separating the rare & slow stuff to this method helps a lot
        private static void CheckRadius(float radius)
        {
            if (radius > MAX_RADIUS)
                Log.Error($"Not enough squares to get to radius {radius}. Max is {MAX_RADIUS}");
        }
    }
}
