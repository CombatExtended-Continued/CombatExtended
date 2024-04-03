using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using System.Reflection.Emit;
using System;
using RimWorld;
using Verse.AI;
using CombatExtended;
using UnityEngine;


namespace CombatExtended.HarmonyCE.Compatibility
{
    [HarmonyPatch]
    class Harmony_Compatibility_BillDoors
    {
        static readonly Assembly ass = AppDomain.CurrentDomain.GetAssemblies().
                                       SingleOrDefault(assembly => assembly.GetName().Name == "BillDoorsFrameworkCE");
        static MethodBase targetMethod = null;

        internal static bool Prepare()
        {
            if (ass == null)
            {
                return false;
            }
            foreach (var t in ass.GetTypes())
            {
                if (t.Name == "DisintegratingProjectileCE")
                {
                    targetMethod = t.GetMethod("Tick", BindingFlags.Public | BindingFlags.Instance);
                }
            }
            if (targetMethod == null)
            {
                Log.Error($"Failed to find target method while attempting to patch Bill Doors' Framework.");
                return false;
            }
            return true;
        }

        internal static MethodBase TargetMethod()
        {
            return targetMethod;
        }

        public static bool Prefix(ref BulletCE __instance)
        {
	    Vector3 v3Pos = __instance.ExactPosition;
	    float distance = (__instance.origin - new Vector2(v3Pos.x, v3Pos.z)).magnitude;
	    float DistancePercent = distance / __instance.equipmentDef.Verbs[0].range;
	    if (DistancePercent <= 1f)
            {
                return true;
            }
            if (DistancePercent > (4 / 3f))
            {
                return true;
            }
            if (!__instance.landed)
            {
                __instance.FlightTicks++;
		var v = __instance.Vec2Position();
		var nextPosition = new Vector3(v.x, __instance.GetHeightAtTicks(__instance.FlightTicks), v.y);
                __instance.ExactPosition = nextPosition;
                if (!__instance.ExactPosition.InBounds(__instance.Map))
                {
                    __instance.Destroy();
                    return false;
                }
                if (__instance.ticksToImpact <= 0)
                {
                    __instance.Destroy();
                }
            }
            return false;
        }
    }
}
