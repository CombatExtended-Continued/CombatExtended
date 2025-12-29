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

        /*
         * Calculates the number of cells (lattice points) within radius.
         * The problem is also called "Gauss's Circle Problem".
         * Read: https://mathworld.wolfram.com/GausssCircleProblem.html
         */
        public static bool NumCellsInRadius(out int __result, float radius)
        {
            // Handle edge cases
            if (radius < 0f)
            {
                __result = 0;
                return false;
            }
            if (radius >= MAX_RADIUS)
            {
                if (radius > MAX_RADIUS)
                {
                    LogNotEnoughSquaresError(radius);
                }
                __result = RadialPatternCount;
                return false;
            }

            /*
             * Estimate the result using area of a circle
             * This estimation is actually really good, with error <= 100 for all radius <= 200
             * See: https://www.desmos.com/calculator/qerpfljbgw
             */
            int idx = (int)(radius * radius * Mathf.PI);

            /*
             * Apply upper bound to avoid IndexOutOfRangeError
             * Subtract 6 so the next step can't raise past the upperbound
             */
            if (idx > RadialPatternCount - 6)
            {
                idx = RadialPatternCount - 6;
            }

            /*
             * Since a circle has 8-way symmetry (axis + diagonals, forming the 8 octants)
             *   the final answers are always the sum of the following:
             * - 1, for the center cell
             * - 4*a, where "a = floor(radius)" is the number of cells on the +x axis
             * - 4*d, where "d = floor(radius * sqrt(1/2))" is the count on the +x+z diagonal
             * - 8*i, where "i" is the number of cells inside the x > z > 0 octant
             * Therefore, if a+d is even, the answer is guaranteed in the form "8n + 1"
             *         and if a+d is odd, the answer is guaranteed in the form "8n + 5"
             * We can determine the last three bits of the answer as 1 or 5 (0b101)
             *   and skip every 8 cells when searching
             */
            const float SqrtHalf = 0.707106781f;
            int a = (int)radius;
            int d = (int)(radius * SqrtHalf);
            idx = (idx & -7) | (((a + d) % 2 == 0) ? 1 : 5);

            // Linear search every 8 cells starting from the middle of estimation
            if (RadialPatternRadii[idx] <= radius)
            {
                do
                {
                    idx += 8; // Search Upward
                }
                while (idx < RadialPatternCount && RadialPatternRadii[idx] <= radius);
                __result = idx;
            }
            else
            {
                do
                {
                    idx -= 8; // Search Downward
                }
                while (idx >= 0 && RadialPatternRadii[idx] > radius);
                __result = idx + 8; // We overshot, so add 8 back
            }
            return false;
        }

        /*
         * A separate method to handle when radius exceeds MAX_RADIUS
         * Apparently loading all these strings for formatting is very slow
         * Even when they are not executed, likely due to branch predictions
         * Separating the rare & slow stuff to this method helps a lot
         */
        private static void LogNotEnoughSquaresError(float radius)
        {
            Log.Error($"Not enough squares to get to radius {radius}. Max is {MAX_RADIUS}");
        }
    }
}
