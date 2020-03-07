using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    [StaticConstructorOnStartup]
    public class CompExplosiveCE : ThingComp
    {
        private class MonoDummy : MonoBehaviour { }

        private const int TicksToSpawnAllFrag = 10;
        private static MonoDummy _monoDummy;

        private const float FragmentShadowChance = 0.2f;

        public CompProperties_ExplosiveCE Props
        {
            get
            {
                return (CompProperties_ExplosiveCE)props;
            }
        }

        static CompExplosiveCE()
        {
            var dummyGO = new GameObject();
            UnityEngine.Object.DontDestroyOnLoad(dummyGO);
            _monoDummy = dummyGO.AddComponent<MonoDummy>();
        }

        private static IEnumerator FragRoutine(Vector3 pos, Map map, float height, Thing instigator, ThingDefCountClass frag, float fragSpeedFactor)
        {
            var cell = pos.ToIntVec3();
            var exactOrigin = new Vector2(pos.x, pos.z);

            //Fragments fly from a 0 (half of a circle) to 45 (3/4 of a circle) degree angle away from the explosion
            var range = new FloatRange(10, 20);
            var fragToSpawn = frag.count;
            var fragPerTick = Mathf.CeilToInt((float)fragToSpawn / TicksToSpawnAllFrag);
            var fragSpawnedInTick = 0;

            while (fragToSpawn > 0)
            {
                var projectile = (ProjectileCE)ThingMaker.MakeThing(frag.thingDef);
                GenSpawn.Spawn(projectile, cell, map);

                projectile.canTargetSelf = true;
                projectile.minCollisionSqr = 1f;
                //TODO : Don't hardcode at FragmentShadowChance, make XML-modifiable
                projectile.castShadow = (UnityEngine.Random.value < FragmentShadowChance);
                projectile.logMisses = false;
                projectile.Launch(
                    instigator,
                    exactOrigin,
                    range.RandomInRange * Mathf.Deg2Rad,
                    UnityEngine.Random.Range(0, 360),
                    height,
                    fragSpeedFactor * projectile.def.projectile.speed,
                    projectile
                );

                fragToSpawn--;
                fragSpawnedInTick++;
                if(fragSpawnedInTick >= fragPerTick)
                {
                    fragSpawnedInTick = 0;
                    yield return new WaitForEndOfFrame();
                }
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
        public virtual void Explode(Thing instigator, Vector3 pos, Map map, float scaleFactor = 1)
        {
            var posIV = pos.ToIntVec3();
            if (map == null)
            {
                Log.Warning("Tried to do explodeCE in a null map.");
                return;
            }
            if (!posIV.InBounds(map))
            {
                Log.Warning("Tried to explodeCE out of bounds");
                return;
            }

            if(!Props.fragments.NullOrEmpty())
            {
                var projCE = parent as ProjectileCE;
                var edifice = posIV.GetEdifice(map);
                var edificeHeight = edifice == null ? 0 : new CollisionVertical(edifice).Max;
                var height = projCE != null ? Mathf.Max(edificeHeight, pos.y) : edificeHeight;

                foreach (var fragment in Props.fragments)
                {
                    _monoDummy.GetComponent<MonoDummy>().StartCoroutine(FragRoutine(pos, map, height, instigator, fragment, Props.fragSpeedFactor));
                }
            }

            // Regular explosion stuff
            if (Props.explosionRadius > 0 && Props.explosionDamage > 0 && parent.def != null && GenGrid.InBounds(posIV, map))
            {
                // Copy-paste from GenExplosion
                Explosion explosion = GenSpawn.Spawn(CE_ThingDefOf.ExplosionCE, posIV, map) as Explosion;
                explosion.height = pos.y;
                explosion.radius = Props.explosionRadius * scaleFactor;
                explosion.damType = Props.explosionDamageDef;
                explosion.instigator = instigator;
                explosion.damAmount = GenMath.RoundRandom(Props.explosionDamage * scaleFactor);
                explosion.weapon = null;
                explosion.projectile = parent.def;
                explosion.preExplosionSpawnThingDef = Props.preExplosionSpawnThingDef;
                explosion.preExplosionSpawnChance = Props.preExplosionSpawnChance;
                explosion.preExplosionSpawnThingCount = Props.preExplosionSpawnThingCount;
                explosion.postExplosionSpawnThingDef = Props.postExplosionSpawnThingDef;
                explosion.postExplosionSpawnChance = Props.postExplosionSpawnChance;
                explosion.postExplosionSpawnThingCount = Props.postExplosionSpawnThingCount;
                explosion.applyDamageToExplosionCellsNeighbors = Props.applyDamageToExplosionCellsNeighbors;
                explosion.armorPenetration = explosion.damAmount * 0.1f;
                explosion.damageFalloff = Props.damageFalloff;
                explosion.chanceToStartFire = Props.chanceToStartFire;
                explosion.StartExplosion(Props.soundExplode ?? Props.explosionDamageDef.soundExplosion, new List<Thing>());
            }
        }
    }
}
