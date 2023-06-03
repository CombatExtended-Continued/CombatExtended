using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Verse;
using CombatExtended.Loader;
using RimWorld;
using System.Collections.Generic;
using HarmonyLib;
using SRTS;
using SPExtended;
using UnityEngine;

namespace CombatExtended.Compatibility.SRTSCompat
{
    [HarmonyPatch(typeof(BomberSkyfaller),
            "DropBomb",
            new Type[] { })]
    [HarmonyPriority(Priority.First)]
    public static class Harmony_BomberSkyfaller_DropBomb
    {
        private static MethodInfo GetCurrentTargetingRadiusInfo = AccessTools.DeclaredMethod(
                typeof(BomberSkyfaller), "GetCurrentTargetingRadius", new Type[] { });

        private static float shotHeight = 5f;

        // Yes, this is a destructive prefix, though with most of the original SRTS code
        public static bool Prefix(BomberSkyfaller __instance)
        {
            ActiveDropPod srts = (ActiveDropPod)__instance.innerContainer.First();
            if(srts == null)
                return false;

            for (int i = 0; i < (__instance.bombType == BombingType.precise
                ? __instance.precisionBombingNumBombs : 1); ++i)
            {
                Thing bombStack = srts.Contents.innerContainer
                    .FirstOrDefault(thing => SRTSMod.mod.settings.allowedBombs.Contains(thing.def.defName));
                if (bombStack == null)
                {
                    return false;
                }

                Thing bombThing = srts.Contents.innerContainer.Take(bombStack, 1);

                IntVec3 bombPos = __instance.bombCells[0];
                if (__instance.bombType == BombingType.carpet)
                {
                    __instance.bombCells.RemoveAt(0);
                }

                object targetingRadiusNullable = GetCurrentTargetingRadiusInfo.Invoke(__instance, null);
                if (targetingRadiusNullable == null)
                {
                    Log.Error("Combat Extended :: SRTSCompat BomberSkyfaller.DropBomb() - "
                            + "could not get SRTS dropship's targeting radius");
                    return false;
                }
                float targetingRadius = (float)(int)targetingRadiusNullable;

                if (bombThing is AmmoThing)
                {
                    // Combat Extended projectile system
                    ThingDef bombProjectileThingDef = (bombThing.def as AmmoDef)?.AmmoSetDefs
                            .Find(ammoSet => ammoSet.ammoTypes?.Any() ?? false)?
                            .ammoTypes
                            .FirstOrDefault()?
                            .projectile;
                    if (bombProjectileThingDef == null)
                    {
                        continue;
                    }

                    ProjectilePropertiesCE bombPropsCE = bombProjectileThingDef.projectile as ProjectilePropertiesCE;
                    if (bombPropsCE == null)
                    {
                        Log.Error("Combat Extended :: SRTSCompat Harmony_BomberSkyfaller_DropBomb Prefix - "
                                + $"AmmoDef {bombThing.def} doesn't have ProjectilePropsCE");
                        return false;
                    }

                    ProjectileCE bombProjectileCE = ThingMaker.MakeThing(bombProjectileThingDef) as ProjectileCE;

                    GenSpawn.Spawn(bombProjectileCE, __instance.DrawPosCell, __instance.Map);
                    bombProjectileCE.canTargetSelf = false;
                    bombProjectileCE.minCollisionDistance = 1;
                    bombProjectileCE.intendedTarget = null;
                    bombProjectileCE.AccuracyFactor = 1f;

                    float maxShotSpeed = targetingRadius / Mathf.Sqrt(2 * shotHeight / bombPropsCE.Gravity);

                    bombProjectileCE.Launch(launcher: __instance,
                            origin: __instance.DrawPosCell.ToIntVec2.ToVector2(),
                            shotAngle: 0f,
                            shotRotation: Rand.Range(-180f, 180f),
                            shotHeight: shotHeight,
                            shotSpeed: maxShotSpeed,
                            equipment: __instance);
                }
                else
                {
                    // Original SRTS bomb dropping system
                    int timerTickExplode = 20 + Rand.Range(0, 5);

                    FallingBomb fallingBombThing = new FallingBomb(bombThing,
                            bombThing.TryGetComp<CompExplosive>(),
                            __instance.Map,
                            __instance.def.skyfaller.shadow);
                    fallingBombThing.HitPoints = int.MaxValue;
                    fallingBombThing.ticksRemaining = timerTickExplode;

                    IntVec3 targetCell = GenRadial.RadialCellsAround(bombPos, targetingRadius, true)
                        .Where(cell => cell.InBounds(__instance.Map))
                        .RandomElementByWeight(cell => 1f - Mathf.Min(cell.DistanceTo(__instance.Position) / targetingRadius, 1f) + 0.05f);

                    fallingBombThing.angle = __instance.angle + (SPTrig.LeftRightOfLine(__instance.DrawPosCell, __instance.Position, targetCell) * -10);
                    fallingBombThing.speed = (float)SPExtra.Distance(__instance.DrawPosCell, targetCell) / fallingBombThing.ticksRemaining;
                    Thing t = GenSpawn.Spawn(fallingBombThing, targetCell, __instance.Map);
                    GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(t, bombThing.TryGetComp<CompExplosive>().Props.explosiveDamageType, null);
                }
            }
            return false;
        }
    }
}
