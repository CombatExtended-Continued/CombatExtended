using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ProjectileImpactFX
{
    public class TrailThrower
    {
        public static void ThrowSmoke(Vector3 loc, float size, Map map, string DefName)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            Rand.PushState();
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDefOf.Mote_Smoke, null);
            moteThrown.Scale = Rand.Range(1.5f, 2.5f) * size;
            moteThrown.rotationRate = Rand.Range(-30f, 30f);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(30, 40), Rand.Range(0.5f, 0.7f));
            Rand.PopState();
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map, WipeMode.Vanish);
        }
    }
}
