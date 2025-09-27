using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SaveOurShip2;
using Verse;

namespace CombatExtended.Compatibility.SOS2Compat;

// Guards ShipInteriorMod2.MoveShip predicates against null Things produced by downstream mods.
[HarmonyPatch]
internal static class Harmony_ShipInteriorMod2_MoveShipNullGuards
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        var nested = AccessTools.Inner(typeof(ShipInteriorMod2), "<>c");
        if (nested == null)
        {
            yield break;
        }

        foreach (var method in nested.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
        {
            if (!method.Name.StartsWith("<MoveShip>b__", StringComparison.Ordinal))
            {
                continue;
            }

            if (method.ReturnType != typeof(bool))
            {
                continue;
            }

            var parameters = method.GetParameters();
            if (parameters.Length != 1 || parameters[0].ParameterType != typeof(Thing))
            {
                continue;
            }

            yield return method;
        }
    }

    private static bool Prefix(ref bool __result, Thing t)
    {
        if (t == null)
        {
            __result = false;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(ShipInteriorMod2), "ReSpawnThingOnMap")]
internal static class Harmony_ShipInteriorMod2_ReSpawnThingOnMap
{
    private static bool Prefix(Thing spawnThing)
    {
        return spawnThing != null;
    }
}
