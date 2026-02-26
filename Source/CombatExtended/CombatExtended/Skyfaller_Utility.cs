using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using RimWorld.Planet;

namespace CombatExtended;
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

    private static GetSkyfallerDrawPos SelectDrawPosMethod(Skyfaller skyfaller) =>
        skyfaller.def.skyfaller.movementType switch
        {
            SkyfallerMovementType.Accelerate => SkyfallerDrawPosUtility.DrawPos_Accelerate,
            SkyfallerMovementType.Decelerate => SkyfallerDrawPosUtility.DrawPos_Decelerate,
            _ => SkyfallerDrawPosUtility.DrawPos_ConstantSpeed
        };

    /// <summary>
    /// Get the current position of a skyfaller, with extrapolated height and adjusted horizontal position.
    /// </summary>
    public static Vector3 CurrentPosition(Skyfaller skyfaller)
    {
        GetSkyfallerDrawPos getDrawPos = SelectDrawPosMethod(skyfaller);
        IntVec3 groundPos = skyfaller.Position;
        Vector3 baseDrawPos = new(groundPos.x, 0, groundPos.z);

        return CalcCurrentPosition(skyfaller, getDrawPos, groundPos, baseDrawPos, skyfaller.ticksToImpact);
    }

    private static Vector3 CalcCurrentPosition(
        Skyfaller skyfaller, GetSkyfallerDrawPos getDrawPos, IntVec3 groundPos, Vector3 baseDrawPos, int tick)
    {
        float speed = CurrentSpeed(skyfaller, tick);
        Vector3 drawPos = getDrawPos(baseDrawPos, tick, skyfaller.angle, speed, false, skyfaller.RandomizeDirectionComp);
        float x = drawPos.x - groundPos.x;
        x *= (1 - cameraSin);
        x += groundPos.x;
        float y = (drawPos.z - groundPos.z) / cameraSin;
        int z = groundPos.z;
        return new Vector3(x, y, z);
    }

    public static IEnumerable<Vector3> PredictPositions(this Skyfaller skyfaller)
    {
        bool leaving = skyfaller.def.skyfaller.reversed;
        int step = -1;
        int end = 0;

        GetSkyfallerDrawPos getDrawPos = SelectDrawPosMethod(skyfaller);
        IntVec3 groundPos = skyfaller.Position;
        Vector3 baseDrawPos = new Vector3(groundPos.x, 0, groundPos.z);

        if (leaving)
        {
            step = 1;
            end = skyfaller.LeaveMapAfterTicks;
        }
        for (int i = skyfaller.ticksToImpact; i != end; i += step)
        {
            yield return CalcCurrentPosition(skyfaller, getDrawPos, groundPos, baseDrawPos, i);
        }
    }

    private delegate Vector3 GetSkyfallerDrawPos(Vector3 baseDrawPos, int tick, float angle, float speed, bool reversed, CompSkyfallerRandomizeDirection offsetComp);
}
