using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using UnityEngine;

namespace CombatExtended.HarmonyCE;
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
        if (radius < 1f) // special case since we start scanning from the *previous* index, which would be negative in this instance.
        {
            __result = 1;
            return false;
        }
        var radialPatternNumCells = RadialPatternNumCells; // cache friendly local references
        var radialPatternRadii = RadialPatternRadii;
        if (radius >= MAX_RADIUS)
        {
            if (radius > MAX_RADIUS)
            {
                Log.Error($"Requested radius {radius} is beyond max. Truncating to {Harmony_GenRadial.MAX_RADIUS}.");
            }
            __result = RadialPatternCount;
            return false;
        }
        float radsq = radius * radius;
        int r = (int)radsq;
        int count = radialPatternNumCells[r - 1];
        /*
          If we raise the max radius above about 200, binary search becomes faster.
          Below 200, the match will reliably be found within 64 tries, where linear memory access dominates.
        int max_count = RadialPatternNumCells[r];
        int span;
        */
#if BINSEARCH
        while ((span = max_count - count) > 64)
        {
            int mid_count = count + span / 2;
            float mid = radialPatternRadii[mid_count];
            if (mid > radius)
            {
                max_count = mid_count;
            }
            else
            {
                count = mid_count;
            }
        }
#endif
        float start = radialPatternRadii[count];
        while (start <= radius)
        {
            start = radialPatternRadii[count++];
        }
        __result = count - 1;
        return false;

    }
}
