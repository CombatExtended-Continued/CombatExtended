using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public static class SightUtility
    {
        private static void Swap<T>(ref T value1, ref T value2)
        {
            var temp = value1;
            value1 = value2;
            value2 = temp;
        }

        private static bool ValidateBounds(out IntVec3 cell, float x, float z, Map map)
        {
            cell = new Vector3(x, 0, z).ToIntVec3();
            return cell.InBounds(map);
        }

        private static float GetFractional(this float value)
        {
            return value / Mathf.Floor(value);
        }

        public static IEnumerable<IntVec3> GetCellsOnLine(Vector3 originPoint, Vector3 targetPoint, Map map)
        {
            IntVec3 validatedCell;
            var x0 = originPoint.x;
            var y0 = originPoint.z;
            var x1 = targetPoint.x;
            var y1 = targetPoint.z;

            var isSteep = Mathf.Abs(y1 - y0) > Mathf.Abs(x1 - x0);

            if (isSteep)
            {
                Swap(ref x0, ref y0);
                Swap(ref x1, ref y1);
            }

            if (x0 > x1)
            {
                Swap(ref x0, ref x1);
                Swap(ref y0, ref y1);
            }

            var dx = x1 - x0;
            var dy = y1 - y0;
            var gradient = dx == 0 ? 1 : dy / dx;

            // First endpoint
            var xEnd = Mathf.Round(x0);
            var yEnd = y0 + gradient * (xEnd - x0);
            var xPixel1 = xEnd;
            var yPixel1 = Mathf.Floor(yEnd);
            if (isSteep)
            {
                if (ValidateBounds(out validatedCell, yPixel1, xPixel1, map))
                    yield return validatedCell;
                if (ValidateBounds(out validatedCell, yPixel1 + 1, xPixel1, map))
                    yield return validatedCell;
            }
            else
            {
                if (ValidateBounds(out validatedCell, xPixel1, yPixel1, map))
                    yield return validatedCell;
                if (ValidateBounds(out validatedCell, xPixel1, yPixel1 + 1, map))
                    yield return validatedCell;
            }

            var interY = yEnd + gradient;

            // Second endpoint
            xEnd = Mathf.Round(x1);
            yEnd = y1 + gradient * (xEnd - x1);
            var xPixel2 = xEnd;
            var yPixel2 = Mathf.Floor(yEnd);
            if (isSteep)
            {
                if (ValidateBounds(out validatedCell, yPixel2, xPixel2, map))
                    yield return validatedCell;
                if (ValidateBounds(out validatedCell, yPixel2 + 1, xPixel2, map))
                    yield return validatedCell;
            }
            else
            {
                if (ValidateBounds(out validatedCell, xPixel2, yPixel2, map))
                    yield return validatedCell;
                if (ValidateBounds(out validatedCell, xPixel2, yPixel2 + 1, map))
                    yield return validatedCell;
            }

            // Main loop
            if (isSteep)
            {
                for (var i = xPixel1 + 1; i <= xPixel2 - 1; i++)
                {
                    if (ValidateBounds(out validatedCell, Mathf.Floor(interY), i, map))
                        yield return validatedCell;
                    if (ValidateBounds(out validatedCell, Mathf.Floor(interY) + 1, i, map))
                        yield return validatedCell;
                    interY += gradient;
                }
            }
            else
            {
                for (var i = xPixel1 + 1; i <= xPixel2 - 1; i++)
                {
                    if (ValidateBounds(out validatedCell, i, Mathf.Floor(interY), map))
                        yield return validatedCell;
                    if (ValidateBounds(out validatedCell, i, Mathf.Floor(interY) + 1, map))
                        yield return validatedCell;
                    interY += gradient;
                }
            }
        }
    }
}
