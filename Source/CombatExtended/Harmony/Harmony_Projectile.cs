using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace CombatExtended.HarmonyCE
{
    public static class Harmony_Projectile
    {
        [HarmonyPatch(typeof(Projectile), nameof(Projectile.Launch), new[] { typeof(Thing), typeof(Vector3), typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(ProjectileHitFlags), typeof(bool), typeof(Thing), typeof(ThingDef)})]
        public static class Harmony_Projectile_Launch
        {
            public static void Prefix(Projectile __instance)
            {
                if (__instance.Spawned && (__instance.def.projectile?.flyOverhead ?? false))
                {
                    __instance.Map.GetComponent<FlyOverProjectileTracker>().Register(__instance);
                }
            }
        }
    }
}

