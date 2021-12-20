using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CombatExtended.CombatExtended.LoggerUtils;
using UnityEngine;
using Verse;
using VFESecurity;

namespace CombatExtended.Compatibility
{
    [StaticConstructorOnStartup]
    public class CETrenches
    {
        private static bool vfeInstalled;
        private const string VFES_ModName = "Vanilla Furniture Expanded - Security";
        static CETrenches()
        {
            vfeInstalled = ModLister.HasActiveModWithName(VFES_ModName);
        }

	private static bool checkVFE(IntVec3 cell, Map map, out float heightAdjust)
	{
	    heightAdjust = 0f;
	    List<Thing> thingList = GridsUtility.GetThingList(cell, map);
	    foreach (Thing thing in thingList) 
	    {
		CompProperties_TerrainSetter compProperties = thing.def.GetCompProperties<CompProperties_TerrainSetter>();
		if (compProperties == null)
		{
		    return false;
		}
		
		TerrainDef terrainDef = compProperties.terrainDef;
		TerrainDefExtension terrainDefExtension = TerrainDefExtension.Get(terrainDef);
		heightAdjust = -terrainDefExtension.coverEffectiveness * CollisionVertical.WallCollisionHeight;
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
	       
            if (vfeInstalled)
            {
		float heightAdjust = 0;
		if (checkVFE(cell, map, out heightAdjust))
		{
		    return heightAdjust;
		}
	    }
	    return 0f;
	}
    }
}
