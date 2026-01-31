using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CombatExtended.CombatExtended.LoggerUtils;
using UnityEngine;
using Verse;
using VFESecurity;

namespace CombatExtended.Compatibility;
[StaticConstructorOnStartup]
public class CETrenches
{
    private static bool CheckTrench(IntVec3 cell, Map map, out float heightAdjust)
    {
        heightAdjust = 0f;

        //consider swimming pawns to be in a trench
        TerrainDef terrain = cell.GetTerrain(map);
        if (terrain != null && terrain.IsWater)
        {
            heightAdjust = terrain.passability == Traversability.Impassable ? -0.75f : -0.5f;
            return true;
        }

        //find trench building
        List<Thing> thingList = cell.GetThingList(map);
        foreach (Thing thing in thingList)
        {
            ModExtensionCover modExtProperties = thing.def.GetModExtension<ModExtensionCover>();
            if (modExtProperties == null || modExtProperties.heightOffset == 0f)
            {
                continue;
            }

            heightAdjust = modExtProperties.heightOffset;
            return true;
        }

        return false;
    }

    public static float GetHeightAdjust(IntVec3 cell, Map map)
    {
        if (cell == null || map == null)
        {
            return 0;
        }

        float heightAdjust = 0;
        
        if (CheckTrench(cell, map, out heightAdjust))
        {
            return heightAdjust;
        }

        return 0f;
    }
}
