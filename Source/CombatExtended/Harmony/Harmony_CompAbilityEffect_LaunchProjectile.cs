using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using CombatExtended;
using RimWorld;
using UnityEngine;


namespace CombatExtended.HarmonyCE;
[HarmonyPatch(typeof(CompAbilityEffect_LaunchProjectile), "LaunchProjectile")]
internal static class Harmony_CompAbilityEffect_LaunchProjectile
{
    internal static bool Prefix(CompAbilityEffect_LaunchProjectile __instance, LocalTargetInfo target)
    {
        Type projectileClass = __instance.Props.projectileDef.thingClass;
        if (projectileClass.IsSubclassOf(typeof(ProjectileCE)) || projectileClass == typeof(ProjectileCE))
        {
            ThingDef projectileDef = __instance.Props.projectileDef.GetProjectile();
            if (projectileDef.projectile is ProjectilePropertiesCE ppce)
            {
                Pawn pawn = __instance.parent.pawn;
                var u = pawn.TrueCenter().WithY((new CollisionVertical(pawn)).shotHeight);
                var targetPos = target.Thing != null ? target.Thing.TrueCenter() : target.Cell.ToVector3Shifted();
                targetPos = targetPos.WithY((new CollisionVertical(target.Thing)).shotHeight);


                var angle = ppce.TrajectoryWorker.ShotAngle(ppce, u, targetPos);
                float shotRotation = ppce.TrajectoryWorker.ShotRotation(ppce, u, targetPos);
                CE_Utility.LaunchProjectileCE(projectileDef, new Vector2(u.x, u.z), target, pawn, angle, shotRotation, u.y, ppce.speed);
                return false;
            }
        }
        return true;
    }
}
