using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;

namespace CombatExtended
{
    [StaticConstructorOnStartup]
    public class CompFragments : ThingComp
    {
        private class MonoDummy : MonoBehaviour { }

        private const int TicksToSpawnAllFrag = 10;

        private static MonoDummy _monoDummy;

        static CompFragments()
        {
            var dummyGO = new GameObject();
            UnityEngine.Object.DontDestroyOnLoad(dummyGO);
            _monoDummy = dummyGO.AddComponent<MonoDummy>();
        }

        public CompProperties_Fragments PropsCE => (CompProperties_Fragments)props;

        private static IEnumerator FragRoutine(Vector3 pos, Map map, float height, Thing instigator, ThingDefCountClass frag, float fragSpeedFactor, float fragShadowChance, FloatRange fragAngleRange, FloatRange fragXZAngleRange)
        {
            var cell = pos.ToIntVec3();
            var exactOrigin = new Vector2(pos.x, pos.z);

            var fragToSpawn = frag.count;
            var fragPerTick = Mathf.CeilToInt((float)fragToSpawn / TicksToSpawnAllFrag);
            var fragSpawnedInTick = 0;

            while (fragToSpawn-- > 0)
            {
                var projectile = (ProjectileCE)ThingMaker.MakeThing(frag.thingDef);
                GenSpawn.Spawn(projectile, cell, map);

                projectile.canTargetSelf = true;
                projectile.minCollisionDistance = 0f;
                projectile.castShadow = (Rand.Value < fragShadowChance);
                projectile.logMisses = false;
                projectile.Launch(
                    instigator,
                    exactOrigin,
                    fragAngleRange.RandomInRange * Mathf.Deg2Rad,
		    (fragXZAngleRange.RandomInRange + 360) % 360,
                    height,
                    fragSpeedFactor * projectile.def.projectile.speed,
                    projectile
                );

                fragSpawnedInTick++;
                if (fragSpawnedInTick >= fragPerTick)
                {
                    fragSpawnedInTick = 0;
                    yield return new WaitForEndOfFrame();
                    if (Find.Maps.IndexOf(map) < 0)
                    {
                        break;
                    }
                }
            }
        }

        public void Throw(Vector3 pos, Map map, Thing instigator, float scaleFactor = 1)
        {
            if (!PropsCE.fragments.NullOrEmpty())
            {
                if (map == null)
                {
                    Log.Warning("CombatExtended :: Tried to throw fragments in a null map.");
                    return;
                }
                if (!pos.ToIntVec3().InBounds(map))
                {
                    Log.Warning("CombatExtended :: Tried to throw fragments out of bounds");
                    return;
                }
		float height;
		FloatRange fragXZAngleRange;
		if (parent is ProjectileCE projCE)
		{
		    height = projCE.Height;
		    fragXZAngleRange = new FloatRange(projCE.shotRotation + PropsCE.fragXZAngleRange.min, projCE.shotRotation + PropsCE.fragXZAngleRange.max);
		}
		else
		{
		    height = 0;
		    fragXZAngleRange = PropsCE.fragXZAngleRange;
		}
		var edifice = pos.ToIntVec3().GetEdifice(map);
                if (edifice != null)
		{
		    var edificeHeight = new CollisionVertical(edifice).Max;
		    height = Mathf.Max(height, edificeHeight);
		}

                foreach (var fragment in PropsCE.fragments)
                {
                    var newCount = fragment;
                    newCount.count = Mathf.RoundToInt(newCount.count * scaleFactor);

                    var routine = FragRoutine(pos, map, height, instigator, fragment, PropsCE.fragSpeedFactor, PropsCE.fragShadowChance, PropsCE.fragAngleRange, fragXZAngleRange);
                    if (!Compatibility.Multiplayer.InMultiplayer)
                        _monoDummy.GetComponent<MonoDummy>().StartCoroutine(routine);
                    else
                    {
                        // Multiplayer really dislikes coroutines
                        while (routine.MoveNext())
                        { }
                    }
                }
            }
        }
    }
}
