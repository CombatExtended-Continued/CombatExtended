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
            return Mathf.Lerp(1f, 0.75f, weight / weightCapacity);
        }

        public static float WorkSpeedFactor(float bulk, float bulkCapacity)
        {
            return Mathf.Lerp(1f, 0.75f, bulk / bulkCapacity);
        }

        public static float EncumberPenalty(float weight, float weightCapacity)
        {
            if (weight > weightCapacity)
                return weight / weightCapacity - 1;
            else
                return 0f;
        }

    }
}
