using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class MassBulkUtility
    {
        public const float WeightCapacityPerBodySize = 35f;
        public const float BulkCapacityPerBodySize = 20f;

        public static float BaseCarryWeight(Pawn p)
        {
            return p.BodySize * WeightCapacityPerBodySize;
        }

        public static float BaseCarryBulk(Pawn p)
        {
            return p.BodySize * BulkCapacityPerBodySize;
        }

        public static float MoveSpeedFactor(float weight, float weightCapacity)
        {
            float t = weight / weightCapacity;
            if (float.IsNaN(t)) t = 1f;

            if (t <= 0.25f)
            {
                return 1f;
            }

            return Mathf.Lerp(1f, 0.75f, t - 0.25f);
        }

        public static float WorkSpeedFactor(float bulk, float bulkCapacity)
        {
            if ((bulk / bulkCapacity) <= 0.35f)
            {
                return 1f;
            }

            return Mathf.Lerp(1f, 0.75f, bulk / bulkCapacity);
        }

        public static float DodgeChanceFactor(float bulk, float bulkCapacity)
        {
            if ((bulk / bulkCapacity) <= 0.5f)
            {
                return 1f;
            }

            return (float)Math.Round(Mathf.Lerp(1f, 0.87f, Math.Min(bulk / bulkCapacity, 1f)), 2);
        }

        public static float HitChanceBulkFactor(float bulk, float bulkCapacity)
        {
            if ((bulk / bulkCapacity) <= 0.35f)
            {
                return 1f;
            }

            return (float)Math.Round(Mathf.Lerp(1f, 0.75f, Math.Min(bulk / bulkCapacity /* - 0.35f*/, 1f)), 2);
        }

        public static float DodgeWeightFactor(float weight, float weightCapacity)
        {
            if ((weight / weightCapacity) <= 0.5f)
            {
                return 1f;
            }

            return (float)Math.Round(Mathf.Lerp(1f, 0.87f, Math.Min(weight / weightCapacity, 1f)), 2);
        }

        public static float EncumberPenalty(float weight, float weightCapacity)
        {
            if (weight > weightCapacity)
            {
                float weightPercent = weight / weightCapacity;
                if (float.IsPositiveInfinity(weightPercent))
                {
                    return 1f;
                }
                return weightPercent - 1;
            }
            else
                return 0f;      
        }

    }
}
