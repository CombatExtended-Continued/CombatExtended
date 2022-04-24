using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;
using CombatExtended;

namespace CombatExtended.Compatibility
{
    public class BulletCE_ThingSpawner : BulletCE
    {
        public AmmoDef_ThingSpawner Def
        {
            get
            {
                return this.def as AmmoDef_ThingSpawner;
            }
        }

        public override void Impact(Thing hitThing)
        {
            base.Impact(hitThing);
            
            Map map = this.launcher.Map;
            IntVec3 cell;
            RCellFinder.TryFindRandomCellNearWith(base.Position, ((IntVec3 x) => x.GetTerrain(map).affordances.Contains(TerrainAffordanceDefOf.Light) && 
                                                                                 x.GetEdifice(map) == null && x.Standable(map)),
                                                                                map, out cell, 2);

            Thing thing = GenSpawn.Spawn(this.Def.spawnDef, cell, map);
            thing.SetFactionDirect( launcher.Faction );

            //CellRect cellRect = CellRect.CenteredOn(base.Position, 2);
            //cellRect.ClipInsideMap(base.Map);
            //for (int i = 0; i < 10; i++)
            //{
            //    IntVec3 randomCell = cellRect.RandomCell;
                
            //}
        }



    }
}
