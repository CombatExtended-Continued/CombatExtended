using System;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class Projectile_FireTrail : ProjectileCE_Explosive
    {
        private int TicksforAppearence = 5;
        public override void Tick()
        {
            base.Tick();
            if (--TicksforAppearence == 0)
            {
                Projectile_FireTrail.ThrowFireTrail(base.Position.ToVector3Shifted(), base.Map, 0.5f);
                TicksforAppearence = 5;
            }
        }
        public static void ThrowFireTrail(Vector3 loc, Map map, float size)
        {
            if (!loc.ShouldSpawnMotesAt(map))
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDef.Named("Mote_Firetrail"), null);
            moteThrown.Scale = Rand.Range(1.5f, 2.5f) * size;
            moteThrown.exactRotation = Rand.Range(-0.5f, 0.5f);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(30, 40), Rand.Range(0.008f, 0.012f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }
    }
}