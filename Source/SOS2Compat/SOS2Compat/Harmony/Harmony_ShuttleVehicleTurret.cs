using System;
using HarmonyLib;
using Vehicles;
using Verse;
using UnityEngine;

namespace CombatExtended.Compatibility.SOS2Compat;
// For SOS2 shuttle turrets, null LaunchProjectileCE so the vanilla launch branch runs; restored in Finalizer.
[HarmonyPatch(typeof(VehicleTurret), "FireTurret")]
public static class Harmony_ShuttleVehicleTurret
{
    private static Func<ThingDef, ThingDef, Def, Vector2, LocalTargetInfo, VehiclePawn, float, float, float, float, object> _savedLaunchProjectileCE;

    public static bool Prepare()
    {
        return true;
    }

    public static void Prefix(VehicleTurret __instance)
    {
        if (!__instance.def.defName.StartsWith("SoS2Shuttle"))
        {
            return;
        }
        _savedLaunchProjectileCE = VehicleTurret.LaunchProjectileCE;
        VehicleTurret.LaunchProjectileCE = null;
    }

    public static void Finalizer(VehicleTurret __instance)
    {
        if (_savedLaunchProjectileCE != null)
        {
            VehicleTurret.LaunchProjectileCE = _savedLaunchProjectileCE;
            _savedLaunchProjectileCE = null;
        }
    }
}
