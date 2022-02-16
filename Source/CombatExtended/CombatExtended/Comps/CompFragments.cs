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

        private const float FragmentShadowChance = 0.2f;
        private const int TicksToSpawnAllFrag = 10;

        private static MonoDummy _monoDummy;

        static CompFragments()
        {
            var dummyGO = new GameObject();
            UnityEngine.Object.DontDestroyOnLoad(dummyGO);
            _monoDummy = dummyGO.AddComponent<MonoDummy>();
        }

        public CompProperties_Fragments PropsCE => (CompProperties_Fragments)props;

        private static IEnumerator FragRoutine(Vector3 pos, Map map, float height, Thing instigator, ThingDefCountClass frag, float fragSpeedFactor)
        {
            var cell = pos.ToIntVec3();
            var exactOrigin = new Vector2(pos.x, pos.z);

            //Fragments fly from a 0 (half of a circle) to 45 (3/4 of a circle) degree angle away from the explosion
            var range = new FloatRange(10, 20);
            var fragToSpawn = frag.count;
            var fragPerTick = Mathf.CeilToInt((float)fragToSpawn / TicksToSpawnAllFrag);
            var fragSpawnedInTick = 0;

            while (fragToSpawn > 0 && Find.Maps.IndexOf(map) >= 0)
            {
                var projectile = (ProjectileCE)ThingMaker.MakeThing(frag.thingDef);
                GenSpawn.Spawn(projectile, cell, map);

                projectile.canTargetSelf = true;
                projectile.minCollisionDistance = 1f;
                //TODO : Don't hardcode at FragmentShadowChance, make XML-modifiable
                projectile.castShadow = (Rand.Value < FragmentShadowChance);
                projectile.logMisses = false;
                projectile.Launch(
                    instigator,
                    exactOrigin,
                    range.RandomInRange * Mathf.Deg2Rad,
                    Rand.Range(0, 360),
                    height,
                    fragSpeedFactor * projectile.def.projectile.speed,
                    projectile
                );

                fragToSpawn--;
                fragSpawnedInTick++;
                if (fragSpawnedInTick >= fragPerTick)
                {
                    fragSpawnedInTick = 0;
                    yield return new WaitForEndOfFrame();
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
                var projCE = parent as ProjectileCE;
                var edifice = pos.ToIntVec3().GetEdifice(map);
                var edificeHeight = edifice == null ? 0f : new CollisionVertical(edifice).Max;
                var height = projCE != null ? Mathf.Max(edificeHeight, projCE.Height) : edificeHeight;

                foreach (var fragment in PropsCE.fragments)
                {
                    var newCount = fragment;
                    newCount.count = Mathf.RoundToInt(newCount.count * scaleFactor);
                    _monoDummy.GetComponent<MonoDummy>().StartCoroutine(FragRoutine(pos, map, height, instigator, fragment, PropsCE.fragSpeedFactor));
                }
            }
        }
    }
}
