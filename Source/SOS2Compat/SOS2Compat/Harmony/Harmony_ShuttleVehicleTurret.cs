using System;
using System.Reflection;
using HarmonyLib;
using Vehicles;
using Verse;
using UnityEngine;

namespace CombatExtended.Compatibility.SOS2Compat;
// nuke LaunchProjectileCE so vanilla launch runs. saved/restored per-call via
// __state, recursion can't stomp it. only for SOS2 shuttles.
[HarmonyPatch(typeof(VehicleTurret), "FireTurret")]
public static class Harmony_ShuttleVehicleTurret
{
    private static readonly Func<VehiclePawn, bool> IsSOS2Shuttle;

    static Harmony_ShuttleVehicleTurret()
    {
        // local ref might be older than runtime SOS2, so grab the helper
        // reflectively
        MethodInfo method = AccessTools.Method("SaveOurShip2.SoS2VehicleUtility:IsSOS2Shuttle", new[] { typeof(VehiclePawn) });
        if (method != null)
        {
            try
            {
                IsSOS2Shuttle = (Func<VehiclePawn, bool>)Delegate.CreateDelegate(typeof(Func<VehiclePawn, bool>), method);
            }
            catch
            {
                IsSOS2Shuttle = null;
            }
        }
    }

    private static bool IsShuttleTurret(VehicleTurret turret)
    {
        return turret.vehicle != null && IsSOS2Shuttle != null && IsSOS2Shuttle(turret.vehicle);
    }

    public static void Prefix(VehicleTurret __instance,
        out Func<ThingDef, ThingDef, Def, Vector2, LocalTargetInfo, VehiclePawn, float, float, float, float, object> __state)
    {
        __state = null;
        if (!IsShuttleTurret(__instance))
        {
            return;
        }
        __state = VehicleTurret.LaunchProjectileCE;
        VehicleTurret.LaunchProjectileCE = null;
    }

    public static void Finalizer(
        Func<ThingDef, ThingDef, Def, Vector2, LocalTargetInfo, VehiclePawn, float, float, float, float, object> __state)
    {
        if (__state != null)
        {
            VehicleTurret.LaunchProjectileCE = __state;
        }
    }
}
