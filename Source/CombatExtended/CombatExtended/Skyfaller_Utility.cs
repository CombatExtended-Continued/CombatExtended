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
            bool leaving = skyfaller.def.skyfaller.reversed;
            int step = -1;
            int end = 0;

            var getDrawPos = SkyfallerDrawPosUtility.DrawPos_ConstantSpeed;
            IntVec3 groundPos = skyfaller.Position;
            Vector3 baseDrawPos = new Vector3(groundPos.x, 0, groundPos.z);
            var angle = skyfaller.angle;
            var cosA = Mathf.Cos(Mathf.Deg2Rad * angle);
            var sinA = Mathf.Sin(Mathf.Deg2Rad * angle);
            var offsetComp = skyfaller.RandomizeDirectionComp;
            if (skyfaller.def.skyfaller.movementType == SkyfallerMovementType.Accelerate)
            {
                getDrawPos = SkyfallerDrawPosUtility.DrawPos_Accelerate;
            }
            else if (skyfaller.def.skyfaller.movementType == SkyfallerMovementType.Decelerate)
            {
                getDrawPos = SkyfallerDrawPosUtility.DrawPos_Decelerate;
            }
            if (leaving)
            {
                step = 1;
                end = skyfaller.LeaveMapAfterTicks;
            }
            for (int i = skyfaller.ticksToImpact; i != end; i += step)
            {
                var speed = CurrentSpeed(skyfaller, i);
                Vector3 drawPos = getDrawPos(baseDrawPos, i, angle, speed, false, offsetComp);
                var x = drawPos.x - groundPos.x;
                x *= (1 - cameraSin);
                x += groundPos.x;
                var y = (drawPos.z - groundPos.z) / cameraSin;
                var z = groundPos.z;
                yield return new Vector3(x, y, z);
            }
        }
    }
}
