using CombatExtended.Loader;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using VanillaGravshipExpanded2;
using Verse;

namespace CombatExtended.Compatibility.VGE2Compat;

[StaticConstructorOnStartup]
public class VGE2Compat : IModPart
{
    private static Harmony harmony;

    public Type GetSettingsType()
    {
        return null;
    }

    public IEnumerable<string> GetCompatList()
    {
        yield break;
    }

    public void PostLoad(ModContentPack content, ISettingsCE _)
    {
        // Equivalent to Projectile_Launch_Patch
        // I wanted to use BlockerRegistry.RegisterCheckForCollisionCallback, but because VGE projectiles are flyOverhead,
        // the CheckCellForCollisionCallback is not called when the projectile flies over VGE_GravshipArmor.
        BlockerRegistry.RegisterCheckForCollisionBetweenCallback(CheckForCollisionBetween); // For gravship armor buildings
        harmony = new Harmony("CombatExtended.Compatibility.VGE2Compat");
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        });
    }

    private static bool CheckForCollisionBetween(ProjectileCE projectile, Vector3 from, Vector3 to)
    {
        // Try to find a wall on the trajectory
        // only handles flyOverhead projectiles for optimization
        if (projectile.launcher?.Faction != null && projectile.def.projectile.flyOverhead)
        {
            // Stolen from ProjectilCE.CheckForCollisionBetween() :)
            IntVec3 lastPosIV3 = from.ToIntVec3();
            IntVec3 newPosIV3 = to.ToIntVec3();
            var cells = GenSight.PointsOnLineOfSight(lastPosIV3, newPosIV3)
                .Union([newPosIV3])
                .Distinct();

            foreach (var cell in cells)
            {
                var building = cell.GetFirstThing(projectile.Map, InternalDefOf.VGE_GravshipArmor);

                // Opposite faction
                if (building != null && building.Faction != null && building.Faction.HostileTo(projectile.launcher.Faction))
                {
                    // impact
                    projectile.ExactPosition = projectile.ExactPosition.Yto0();
                    projectile.Impact(building);
                    return true;
                }
            }
        }
        return false;
    }
}
