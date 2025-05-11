using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using RimWorld.Planet;

namespace CombatExtended
{
    public static class Skyfaller_Utility
    {
        public const float cameraSin = 0.84f; // Sin(57 degrees)
        public static float CurrentSpeed(Skyfaller skyfaller, int tick)
        {
            if (skyfaller.def.skyfaller.speedCurve == null)
            {
                return skyfaller.def.skyfaller.speed;
            }
            return skyfaller.def.skyfaller.speedCurve.Evaluate(TimeInAnimation(skyfaller, tick)) * skyfaller.def.skyfaller.speed;
        }

        public static float TimeInAnimation(Skyfaller skyfaller, int tick)
        {
            if (skyfaller.def.skyfaller.reversed)
            {
                return (float)tick / (float)skyfaller.LeaveMapAfterTicks;
            }
            return 1f - (float)tick / (float)skyfaller.ticksToImpactMax;
        }

        public static IEnumerable<Vector3> PredictPositions(this Skyfaller skyfaller, int maxTicks)
        {
            for (int i = 1; i <= maxTicks && skyfaller.ticksToImpact - i >= 0; i++)
            {
                try
                {
                    skyfaller.ticksToImpact -= i;
                    yield return skyfaller.DrawPos;
                }
                finally
                {
                    skyfaller.ticksToImpact += i;
                }
            }
        }
    }
}
