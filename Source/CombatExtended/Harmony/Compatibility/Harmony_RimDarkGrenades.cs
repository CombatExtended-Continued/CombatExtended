using System.Reflection;
using HarmonyLib;
using Rimworld
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace CombatExtended.HarmonyCE.Compatibility;

public class RimDarkGrenadeExtension : DefModExtension
{
}

[HarmonyPatch]
public static class Harmony_RimDarkGrenades
{
    private static MethodBase targetMethod;

    public static bool Prepare()
    {
        if (AccessTools.TypeByName("Genes40k.Building_DropDrop") == null)
        {
            return false;
        }
        var type = AccessTools.TypeByName("VEF.Abilities.Ability_ShootProjectile");
        targetMethod = type == null ? null : AccessTools.Method(type, "ShootProjectile", new[] { typeof(GlobalTargetInfo) });
        return targetMethod != null;
    }

    public static MethodBase TargetMethod() => targetMethod;

    public static bool Prefix(VEF.Abilities.Ability_ShootProjectile __instance, GlobalTargetInfo target, ref Projectile __result)
    {
        if (__instance.def.GetModExtension<RimDarkGrenadeExtension>() == null)
        {
            return true;
        }
        var projectileDef = __instance.def.GetModExtension<VEF.Abilities.AbilityExtension_Projectile>()?.projectile;
        if (projectileDef?.projectile is not ProjectilePropertiesCE properties
            || !typeof(ProjectileCE).IsAssignableFrom(projectileDef.thingClass))
        {
            return true;
        }

        // Intercept only the throw. The native Cast methods still handle
        // cooldowns, paired grenade charges, effects and save data.
        __result = null; // VEF's Cast ignores this result; CE projectiles are not Verse.Projectile.
        var pawn = __instance.pawn;
        if (pawn == null || !pawn.Spawned || !target.Cell.InBounds(pawn.Map))
        {
            return false;
        }
        var origin = pawn.TrueCenter().WithY(new CollisionVertical(pawn).shotHeight);
        // Grenade abilities target the ground, including when a pawn is selected.
        var destination = target.Cell.ToVector3Shifted().WithY(0);
        var localTarget = new LocalTargetInfo(target.Cell);
        var angle = properties.TrajectoryWorker.ShotAngle(properties, origin, destination);
        var rotation = properties.TrajectoryWorker.ShotRotation(properties, origin, destination);
        CE_Utility.LaunchProjectileCE(projectileDef, new Vector2(origin.x, origin.z), localTarget,
            pawn, angle, rotation, origin.y, properties.speed);
        return false;
    }
}
