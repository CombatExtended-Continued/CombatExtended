using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class CompExplosiveCE : ThingComp
    {
        public CompProperties_ExplosiveCE Props
        {
            get
            {
                return (CompProperties_ExplosiveCE)props;
            }
        }

        /// <summary>
        /// Produces a secondary explosion on impact using the explosion values from the projectile's projectile def. Requires the projectile's launcher to be passed on due to protection level, 
        /// only works when parent can be cast as ProjectileCE. Intended use is for HEAT and similar weapons that spawn secondary explosions while also penetrating, NOT explosive ammo of 
        /// anti-materiel rifles as the explosion just spawns on top of the pawn, not inside the hit body part.
        /// 
        /// Additionally handles fragmentation effects if defined.
        /// </summary>
        /// <param name="instigator">Launcher of the projectile calling the method</param>
		public virtual void Explode(Thing instigator, IntVec3 pos, Map map)
        {
            if (map == null)
            {
                Log.Warning("Tried to do explodeCE in a null map.");
                return;
            }
            // Regular explosion stuff
            if (Props.explosionRadius > 0 && Props.explosionDamage > 0 && parent.def != null && GenGrid.InBounds(pos, map))
            {
				GenExplosion.DoExplosion
                    (pos,
					map,
					Props.explosionRadius,
					Props.explosionDamageDef,
					instigator,
					Props.soundExplode ?? Props.explosionDamageDef.soundExplosion,
					parent.def, 
					null,
					Props.postExplosionSpawnThingDef = null,
					Props.postExplosionSpawnChance = 0f,
					Props.postExplosionSpawnThingCount = 1, 
					Props.applyDamageToExplosionCellsNeighbors = false, 
					Props.preExplosionSpawnThingDef = null, 
					Props.explosionSpawnChance = 0,
					Props.preExplosionSpawnThingCount);
            }
            // Fragmentation stuff
            if (!Props.fragments.NullOrEmpty() && GenGrid.InBounds(pos, map))
            {
                if (Props.fragRange <= 0)
                {
                    Log.Error(parent.LabelCap + " has fragments but no fragRange");
                }

                else
                {
                    Vector2 exactOrigin = new Vector2(parent.DrawPos.x, parent.DrawPos.z);
                    float height = CE_Utility.GetCollisionVertical(pos.GetEdifice(map), true).max;
                    
                    foreach (ThingCountClass fragment in Props.fragments)
                    {
                        for (int i = 0; i < fragment.count; i++)
                        {
                            ProjectileCE projectile = (ProjectileCE)ThingMaker.MakeThing(fragment.thingDef, null);
                            GenSpawn.Spawn(projectile, pos, map);
                            
                            var projCE = parent as ProjectileCE;
                            if (projCE != null)
                            {
                            	height = Mathf.Max(height, projCE.Height);
                            }
                            
                			projectile.minCollisionSqr = 1f;
                            projectile.Launch(instigator, exactOrigin, UnityEngine.Random.Range(-Mathf.PI / 2f, Mathf.PI / 2f), UnityEngine.Random.Range(0, 360), height);
                        }
                    }
                }
            }
        }
    }
}
