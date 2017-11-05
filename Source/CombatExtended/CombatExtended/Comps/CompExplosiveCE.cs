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
        /// Produces a secondary explosion on impact using the explosion values from the projectile's projectile def. Requires the projectile's launcher to be passed on due to protection level. 
        /// Intended use is for HEAT and similar weapons that spawn secondary explosions while also penetrating, NOT explosive ammo of anti-materiel rifles as the explosion just spawns 
        /// on top of the pawn, not inside the hit body part.
        /// 
        /// Additionally handles fragmentation effects if defined.
        /// </summary>
        /// <param name="instigator">Launcher of the projectile calling the method</param>
		public virtual void Explode(Thing instigator, IntVec3 pos, Map map, float scaleFactor = 1)
        {
            if (map == null)
            {
                Log.Warning("Tried to do explodeCE in a null map.");
                return;
            }
            if (!pos.InBounds(map))
            {
                Log.Warning("Tried to explodeCE out of bounds");
                return;
            }

            // Fragmentation stuff
            if (!Props.fragments.NullOrEmpty() && GenGrid.InBounds(pos, map))
            {
                Vector2 exactOrigin = new Vector2(parent.DrawPos.x, parent.DrawPos.z);
                float height = (new CollisionVertical(pos.GetEdifice(map))).Max;
                
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
                        
                        projectile.canTargetSelf = true;
            			projectile.minCollisionSqr = 1f;
                        projectile.Launch(instigator, exactOrigin, UnityEngine.Random.Range(0, Mathf.PI / 8f), UnityEngine.Random.Range(0, 360), height, Props.fragSpeedFactor * projectile.def.projectile.speed);
                    }
                }
            }
            // Regular explosion stuff
            if (Props.explosionRadius > 0 && Props.explosionDamage > 0 && parent.def != null && GenGrid.InBounds(pos, map))
            {
                // Can't use GenExplosion because it no longer allows setting damage amount

                // Copy-paste from GenExplosion
                Explosion explosion = (Explosion)GenSpawn.Spawn(ThingDefOf.Explosion, pos, map);
                explosion.radius = Props.explosionRadius * scaleFactor;
                explosion.damType = Props.explosionDamageDef;
                explosion.instigator = instigator;
                explosion.damAmount = GenMath.RoundRandom(Props.explosionDamage * scaleFactor);
                explosion.weapon = null;
                explosion.preExplosionSpawnThingDef = Props.preExplosionSpawnThingDef;
                explosion.preExplosionSpawnChance = Props.explosionSpawnChance;
                explosion.preExplosionSpawnThingCount = Props.preExplosionSpawnThingCount;
                explosion.postExplosionSpawnThingDef = Props.postExplosionSpawnThingDef;
                explosion.postExplosionSpawnChance = Props.postExplosionSpawnChance;
                explosion.postExplosionSpawnThingCount = Props.postExplosionSpawnThingCount;
                explosion.applyDamageToExplosionCellsNeighbors = Props.applyDamageToExplosionCellsNeighbors;
                explosion.dealMoreDamageAtCenter = true;

                explosion.StartExplosion(Props.soundExplode ?? Props.explosionDamageDef.soundExplosion);
            }
        }
    }
}
