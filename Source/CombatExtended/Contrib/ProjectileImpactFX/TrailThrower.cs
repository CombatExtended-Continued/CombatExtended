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
        // Token: 0x060051BB RID: 20923 RVA: 0x001B86DC File Offset: 0x001B68DC
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

        // Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00001050
        public static void ThrowSmokeTrail(Vector3 loc, float size, Map map, string DefName)
        {
            MoteCounter moteCounter = new MoteCounter();
            bool flag = !loc.ShouldSpawnMotesAt(map) || moteCounter.SaturatedLowPriority;
            if (!flag)
            {
                Rand.PushState();
                MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDef.Named(DefName), null);
                moteThrown.Scale = Rand.Range(2f, 3f) * size;
                moteThrown.exactPosition = loc;
                moteThrown.rotationRate = Rand.Range(-0.5f, 0.5f);
                moteThrown.SetVelocity((float)Rand.Range(30, 40), Rand.Range(0.008f, 0.012f));
                Rand.PopState();
                GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map, WipeMode.Vanish);
            }
        }
    }
}
