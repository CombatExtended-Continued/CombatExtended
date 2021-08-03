using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public static class GenSightCE
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

            while (IsPositionWithinVector(currentPos, endPos, stepX, stepZ))
            {
                //Find next X-axis cell in vector's path
                var nextXPointDistance = (float)stepX;
                var currentPosXDecimalPortion = currentPos.x % 1;
                if (!NearlyEqual(currentPosXDecimalPortion, 0f) && !NearlyEqual(Mathf.Abs(currentPosXDecimalPortion), 1f))
                {
                    nextXPointDistance = ((currentPosXDecimalPortion >= 0.5f) ? 1f - currentPosXDecimalPortion : currentPosXDecimalPortion) * stepX;
                }
                var nextXCell = new Vector3(
                    currentPos.x + nextXPointDistance,
                    currentPos.y,
                    currentPos.z + (nextXPointDistance / xzRatio)
                );

                //Find next Z-axis cell in vector's path
                var nextZPointDistance = (float)stepZ;
                var currentPosZDecimalPortion = currentPos.z % 1;
                if (!NearlyEqual(currentPosZDecimalPortion, 0f) && !NearlyEqual(Mathf.Abs(currentPosZDecimalPortion), 1f))
                {
                    nextZPointDistance = ((currentPosZDecimalPortion >= 0.5f) ? 1f - currentPosZDecimalPortion : currentPosZDecimalPortion) * stepZ;
                }
                var nextZCell = new Vector3(
                    currentPos.x + (nextZPointDistance * xzRatio),
                    currentPos.y,
                    currentPos.z + nextZPointDistance
                );

                //Pick whichever of the new cells is closest to the current cell
                currentPos = (GetDistanceSqr(currentPos, nextXCell) < GetDistanceSqr(currentPos, nextZCell)) ? nextXCell : nextZCell;

                if (IsPositionWithinVector(currentPos, endPos, stepX, stepZ))   //sanity check to avoid overshooting the target and checking unnecesary cells
                {
                    //the 0.0001f offsets to X and Z are to ensure we select the correct cell when traversing the vector in a negative direction.
                    yield return new IntVec3(
                        (int)(currentPos.x + (0.0001f * stepX)),
                        (int)currentPos.y,
                        (int)(currentPos.z + (0.0001f * stepZ))
                    );

                    //If the current position is a corner, we need not only the 2 cells along the vector's path,
                    //but also the other two touching the corner. (prevents diagonal LOS through walls)
                    if (NearlyEqual(currentPos.x, Mathf.RoundToInt(currentPos.x)) && NearlyEqual(currentPos.z, Mathf.RoundToInt(currentPos.z)))
                    {
                        yield return new IntVec3(
                            (int)(currentPos.x + (0.0001f * stepX) - stepX),
                            (int)currentPos.y,
                            (int)(currentPos.z + (0.0001f * stepZ))
                        );
                        yield return new IntVec3(
                            (int)(currentPos.x + (0.0001f * stepX)),
                            (int)currentPos.y,
                            (int)(currentPos.z + (0.0001f * stepZ) - stepZ)
                        );
                    }
                }
            }
        }

        public static IEnumerable<IntVec3> PartialLineOfSights(this Pawn pawn, LocalTargetInfo targetFacing)
        {
            Map map = pawn.Map;
            IntVec3 startPos = pawn.Position;
            IntVec3 endPos = targetFacing.Cell;
            foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(startPos, new IntVec3(
                    (int)((startPos.x * 3 + endPos.x) / 4f),
                    (int)((startPos.y * 3 + endPos.y) / 4f),
                    (int)((startPos.z * 3 + endPos.z) / 4f))))
            {
                Thing cover = cell.GetCover(map);
                if (cover != null && cover.def.Fillage == FillCategory.Full)
                    yield break;
                yield return cell;
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
        private static bool IsPositionWithinVector(Vector3 currentPos, Vector3 endPos, int stepX, int stepZ)
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

        private static bool NearlyEqual(float a, float b, float tolerance = 0.0001f)
        {
            if (a == b) // shortcut, handles infinities
            {
                return true;
            }
            return Math.Abs(a - b) < tolerance;
        }

    }
}
