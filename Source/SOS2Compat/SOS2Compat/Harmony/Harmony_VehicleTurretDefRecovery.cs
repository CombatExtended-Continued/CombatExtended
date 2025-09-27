using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Vehicles;
using Verse;

namespace CombatExtended.Compatibility.SOS2Compat;

[HarmonyPatch(typeof(VehicleTurret), nameof(VehicleTurret.PostPostLoadInit))]
internal static class Harmony_VehicleTurret_PostPostLoadInit
{
    private static void Postfix(VehicleTurret __instance)
    {
        VehicleTurretDefRecovery.TryEnsureTurretDef(__instance);
    }
}

[HarmonyPatch(typeof(VehicleTurret), nameof(VehicleTurret.FireTurretCE))]
internal static class Harmony_VehicleTurret_FireTurretCE
{
    private static void Prefix(VehicleTurret __instance)
    {
        VehicleTurretDefRecovery.TryEnsureTurretDef(__instance);
    }
}

internal static class VehicleTurretDefRecovery
{
    private const string LogPrefix = "CombatExtended SOS2Compat ::";
    private static readonly FieldInfo LegacyTurretDefField = AccessTools.Field(typeof(VehicleTurret), "turretDef");
    private static readonly PropertyInfo LegacyTurretDefProperty = AccessTools.Property(typeof(VehicleTurret), "turretDef");

    internal static void TryEnsureTurretDef(VehicleTurret turret)
    {
        if (turret?.def != null || turret == null)
        {
            return;
        }

        var vehicle = turret?.vehicle;
        if (vehicle == null)
        {
            return;
        }

        if (TryResolveFromReference(turret))
        {
            return;
        }

        if (TryResolveFromVehicleDef(turret, vehicle))
        {
            return;
        }

        if (TryResolveFromUpgradeTree(turret, vehicle))
        {
            return;
        }

        if (TryResolveFromDefDatabase(turret))
        {
            return;
        }

        Log.WarningOnce($"{LogPrefix} Unable to resolve VehicleTurretDef for turret key '{turret?.key ?? "<null>"}' on vehicle '{vehicle.LabelShort}'.", turret.GetHashCode() ^ 0x6CEC1B17);
    }

    private static void AssignDef(VehicleTurret turret, VehicleTurretDef def)
    {
        if (def == null)
        {
            return;
        }

        turret.def = def;
        if (LegacyTurretDefField != null)
        {
            LegacyTurretDefField.SetValue(turret, def);
        }
        else if (LegacyTurretDefProperty?.CanWrite == true)
        {
            LegacyTurretDefProperty.SetValue(turret, def);
        }
    }

    private static bool TryResolveFromReference(VehicleTurret turret)
    {
        if (turret?.reference?.def != null)
        {
            AssignDef(turret, turret.reference.def);
            return true;
        }

        if (turret == null)
        {
            return false;
        }

        var legacy = LegacyTurretDefField?.GetValue(turret) as VehicleTurretDef ??
                     LegacyTurretDefProperty?.GetValue(turret) as VehicleTurretDef;
        if (legacy != null)
        {
            AssignDef(turret, legacy);
            return true;
        }

        return false;
    }

    private static bool TryResolveFromVehicleDef(VehicleTurret turret, VehiclePawn vehicle)
    {
        var props = vehicle.CompVehicleTurrets?.Props;
        if (props?.turrets == null)
        {
            return false;
        }

        foreach (var proto in props.turrets)
        {
            if (KeysMatch(proto.key, turret.key) && proto.def != null)
            {
                AssignDef(turret, proto.def);
                return true;
            }
        }

        return false;
    }

    private static bool TryResolveFromUpgradeTree(VehicleTurret turret, VehiclePawn vehicle)
    {
        var compUpgradeTree = vehicle.CompUpgradeTree;
        var upgradeTree = compUpgradeTree?.Props?.def;
        if (upgradeTree?.nodes == null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(turret.upgradeKey))
        {
            var node = upgradeTree.nodes.FirstOrDefault(n => KeysMatch(n.key, turret.upgradeKey));
            if (TryResolveFromNode(turret, node))
            {
                return true;
            }
        }

        foreach (var node in upgradeTree.nodes)
        {
            if (TryResolveFromNode(turret, node))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryResolveFromNode(VehicleTurret turret, UpgradeNode node)
    {
        if (node?.upgrades == null)
        {
            return false;
        }

        foreach (var upgrade in node.upgrades)
        {
            if (upgrade is not TurretUpgrade turretUpgrade || turretUpgrade.turrets == null)
            {
                continue;
            }

            foreach (var proto in turretUpgrade.turrets)
            {
                if (KeysMatch(proto.key, turret.key) && proto.def != null)
                {
                    AssignDef(turret, proto.def);
                    return true;
                }

                if (!string.IsNullOrEmpty(proto.def?.defName) &&
                    KeyMatchesDefName(turret, proto.def.defName))
                {
                    AssignDef(turret, proto.def);
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryResolveFromDefDatabase(VehicleTurret turret)
    {
        foreach (var candidate in CandidateNames(turret))
        {
            if (string.IsNullOrEmpty(candidate))
            {
                continue;
            }

            var defFromKey = DefDatabase<VehicleTurretDef>.GetNamedSilentFail(candidate);
            if (defFromKey != null)
            {
                AssignDef(turret, defFromKey);
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> CandidateNames(VehicleTurret turret)
    {
        if (turret == null)
        {
            yield break;
        }

        if (!string.IsNullOrEmpty(turret.key))
        {
            yield return turret.key;
        }

        if (!string.IsNullOrEmpty(turret.groupKey))
        {
            yield return turret.groupKey;
        }

        if (!string.IsNullOrEmpty(turret.upgradeKey))
        {
            yield return turret.upgradeKey;
        }

        foreach (var raw in new[] { turret.key, turret.groupKey })
        {
            if (string.IsNullOrEmpty(raw))
            {
                continue;
            }

            var parts = raw.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                yield return parts[0];
                yield return string.Join("_", parts.Take(parts.Length - 1));
            }
        }
    }

    private static bool KeysMatch(string a, string b)
    {
        return !string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b) && a.Equals(b, StringComparison.Ordinal);
    }

    private static bool KeyMatchesDefName(VehicleTurret turret, string defName)
    {
        if (string.IsNullOrEmpty(defName))
        {
            return false;
        }

        return KeysMatch(defName, turret.key) ||
               KeysMatch(defName, turret.groupKey) ||
               KeysMatch(defName, turret.upgradeKey);
    }
}
