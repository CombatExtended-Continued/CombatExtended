using CombatExtended.Loader;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
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
        BlockerRegistry.RegisterCheckForCollisionCallback(CheckCollision); // For gravship armor buildings
        harmony = new Harmony("CombatExtended.Compatibility.VGE2Compat");
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        });
    }

    private static bool CheckCollision(ProjectileCE projectile, IntVec3 cell, Thing launcher) {
        // Try to find a wall on the cell
        if (launcher?.Faction != null)
        {
            // Find a gravship armor building on the cell
            var building = cell.GetFirstThing(projectile.Map, InternalDefOf.VGE_GravshipArmor);
            // Opposite faction
            if (building != null && building.Faction != null && building.Faction.HostileTo(launcher.Faction))
            {
                // impact
                return true;
            }
        }
        return false;
    }
}
