using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using CombatExtended;
using RimWorld;
using UnityEngine;


namespace CombatExtended.HarmonyCE

{
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
                    var u = pawn.TrueCenter();
                    var sourceLoc = new Vector2();
                    sourceLoc.Set(u.x, u.z);
                    var targetLocation = new Vector2();
                    targetLocation.Set(target.Pawn.TrueCenter().x, target.Pawn.TrueCenter().z);

                    var w = (targetLocation - sourceLoc);
                    float shotRotation = (-90 + Mathf.Rad2Deg * Mathf.Atan2(w.y, w.x)) % 360;

                    var angle = ProjectileCE.GetShotAngle(projectileDef.projectile.speed, (target.Cell - pawn.Position).LengthHorizontal, 0, false, ppce.Gravity);
                    CE_Utility.LaunchProjectileCE(projectileDef, sourceLoc, target, pawn, angle, shotRotation, 1, 80);
                    return false;
                }
            }
            return true;
        }
    }
}
