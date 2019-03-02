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

        private static float GetFractional(this float value)
        {
            return value / Mathf.Floor(value);
        }

        public static IEnumerable<IntVec3> GetCellsOnLine(Vector3 originPoint, Vector3 targetPoint)
        {
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
            var xGap = 1 - (x0 + 0.5f).GetFractional();
            var xPixel1 = xEnd;
            var yPixel1 = Mathf.Floor(yEnd);
            if (isSteep)
            {
                yield return new Vector3(yPixel1, 0, xPixel1).ToIntVec3();
                yield return new Vector3(yPixel1 + 1, 0, xPixel1).ToIntVec3();
            }
            else
            {
                yield return new Vector3(xPixel1, 0, yPixel1).ToIntVec3();
                yield return new Vector3(xPixel1, 0, yPixel1 + 1).ToIntVec3();
            }

            var interY = yEnd + gradient;

            // Second endpoint
            xEnd = Mathf.Round(x1);
            yEnd = y1 + gradient * (xEnd - x1);
            xGap = (x1 + 0.5f).GetFractional();
            var xPixel2 = xEnd;
            var yPixel2 = Mathf.Floor(yEnd);
            if (isSteep)
            {
                yield return new Vector3(yPixel2, 0, xPixel2).ToIntVec3();
                yield return new Vector3(yPixel2 + 1, 0, xPixel2).ToIntVec3();
            }
            else
            {
                yield return new Vector3(xPixel2, 0, yPixel2).ToIntVec3();
                yield return new Vector3(xPixel2, yPixel2 + 1).ToIntVec3();
            }

            // Main loop
            if (isSteep)
            {
                for (var i = xPixel1 + 1; i <= xPixel2 - 1; i++)
                {
                    yield return new Vector3(Mathf.Floor(interY), 0, i).ToIntVec3();
                    yield return new Vector3(Mathf.Floor(interY) + 1, 0, i).ToIntVec3();
                    interY += gradient;
                }
            }
            else
            {
                for (var i = xPixel1 + 1; i <= xPixel2 - 1; i++)
                {
                    yield return new Vector3(i, 0, Mathf.Floor(interY)).ToIntVec3();
                    yield return new Vector3(i, 0, Mathf.Floor(interY) + 1).ToIntVec3();
                    interY += gradient;
                }
            }
        }
    }
}