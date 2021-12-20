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
        public static float GetHeightAdjust(IntVec3 cell, Map map)
        {
            if (cell == null || map == null)
            {
                return 0;
            }
	       
            if (vfeInstalled)
            {
                List<Thing> thingList = GridsUtility.GetThingList(cell, map);
                foreach (Thing thing in thingList) 
                {
		    CompProperties_TerrainSetter compProperties = thing.def.GetCompProperties<CompProperties_TerrainSetter>();
                    if (compProperties == null)
                    {
                        return 0f;
                    }
		    
                    TerrainDef terrainDef = compProperties.terrainDef;
                    TerrainDefExtension terrainDefExtension = TerrainDefExtension.Get(terrainDef);
                    return terrainDefExtension.coverEffectiveness * -1.76;
		    
		}
	    }
	    return 0f;
	}
    }
}
