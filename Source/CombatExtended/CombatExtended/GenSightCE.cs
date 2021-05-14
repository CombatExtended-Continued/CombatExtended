using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    class GenSightCE
    {
        /// <summary>
        /// Equivalent of Verse.GenSight.PointsOnLineOfSight, with support for floating-point vectors.
        /// Allows for better precision when calculating cells on a shot line's path (e.g: leaning pawns).
        /// </summary>
        /// <param name="startPos">Origin coordinate.</param>
        /// <param name="endPos">Destination coordinate.</param>
        /// <returns>List of cells that intersect the shot line.</returns>
        public static IEnumerable<IntVec3> PointsOnLineOfSight(Vector3 startPos, Vector3 endPos)
        {
            if (startPos.x == endPos.x || startPos.z == endPos.z)
            {
                foreach (var cell in PointsOnStraightLineOfSight(startPos, endPos))
                {
                    yield return cell;
                }
                yield break;
            }

            Vector3 losRay = endPos - startPos;
            Vector3 currentPos = new Vector3(startPos.x, startPos.y, startPos.z);
            int stepX = (losRay.x >= 0) ? 1 : -1;
            int stepZ = (losRay.z >= 0) ? 1 : -1;
            float xzRatio = losRay.x / losRay.z;

            yield return startPos.ToIntVec3();

            while (IterateThroughLine(currentPos, endPos, stepX, stepZ))
            {
                var nextXPoint = new Vector3(currentPos.x + stepX, currentPos.y, currentPos.z + (stepX / xzRatio));
                var nextZPoint = new Vector3(currentPos.x + (stepZ * xzRatio), currentPos.y, currentPos.z + stepZ);
                currentPos = (GetDistanceSqr(currentPos, nextXPoint) < GetDistanceSqr(currentPos, nextZPoint)) ? nextXPoint : nextZPoint;
                yield return currentPos.ToIntVec3();
            }
        }

        /// <summary>
        /// Helper function of GenSightCE.PointsOnLineOfSight, using simplified calculations for vertical/horizontal lines.
        /// </summary>
        /// <param name="startPos">Origin coordinate.</param>
        /// <param name="endPos">Destination coordinate.</param>
        /// <returns>List of cells that intersect the shot line.</returns>
        private static IEnumerable<IntVec3> PointsOnStraightLineOfSight(Vector3 startPos, Vector3 endPos)
        {
            IntVec3 currentCell = startPos.ToIntVec3();
            if (startPos.x == endPos.x)
            {
                //Vertical line
                int stepZ = (endPos.z - startPos.z >= 0) ? 1 : -1;
                float currentZ = startPos.z;
                while ((stepZ > 0) ? currentZ <= endPos.z : currentZ >= endPos.z)
                {
                    currentCell.z = (int)currentZ;
                    yield return currentCell;
                    currentZ += stepZ;
                }
            }
            else if (startPos.z == endPos.z)
            {
                //Horizontal line
                int stepX = (endPos.x - startPos.x >= 0) ? 1 : -1;
                float currentX = startPos.x;
                while ((stepX > 0) ? currentX <= endPos.x : currentX >= endPos.x)
                {
                    currentCell.x = (int)currentX;
                    yield return currentCell;
                    currentX += stepX;
                }
            }
            else
            {
                Log.Error("[CE] GenSightCE.PointsOnStraightLineOfSight was given a diagonal vector, should use GenSightCE.PointsOnLineOfSight instead.");
            }
        }

        /// <summary>
        /// Checks whether the current coordinate is past the target coordinate, based on the intended X and Z axis directions.
        /// </summary>
        /// <param name="currentPos">Current coordinate.</param>
        /// <param name="endPos">Destination coordinate.</param>
        /// <param name="stepX">Movement direction along the X axis.</param>
        /// <param name="stepZ">Movement direction along the Z axis.</param>
        /// <returns>True if there are still cells left to iterate along the vector path, false otherwise.</returns>
        private static bool IterateThroughLine(Vector3 currentPos, Vector3 endPos, int stepX, int stepZ)
        {
            if ((stepX > 0 && currentPos.x >= endPos.x) || (stepX < 0 && currentPos.x <= endPos.x))
            {
                return false;
            }
            if ((stepZ > 0 && currentPos.z >= endPos.z) || (stepZ < 0 && currentPos.z <= endPos.z))
            {
                return false;
            }
            return true;
        }

        private static double GetDistanceSqr(Vector3 v1, Vector3 v2)
        {
            return Math.Pow(v2.x - v1.x, 2) + Math.Pow(v2.z - v1.z, 2);
        }

    }
}
