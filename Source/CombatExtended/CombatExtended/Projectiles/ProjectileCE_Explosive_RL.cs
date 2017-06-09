using System;
using Verse;
using UnityEngine;
using RimWorld;

namespace CombatExtended
{
    public class ProjectileCE_Explosive_RL : ProjectileCE_Explosive
    {
        private int Burnticks = 3;
        public override void Tick()
        {
            Map map = base.Map;
            if (--Burnticks == 0)
            {
                ThrowSmokeForRocketsandMortars(base.Position.ToVector3Shifted(), 1f);
                ThrowRocketExhaustFlame(base.Position.ToVector3Shifted(), 2f);
                Burnticks = 3;
            }
            base.Tick();
        }

        public override void SpawnSetup(Map map)
        {
            base.SpawnSetup(map);
            ThrowSmokeForRocketsandMortars(base.Position.ToVector3Shifted(), 4f);
            ThrowRocketExhaustFlame(base.Position.ToVector3Shifted(), 1f);
        }

        public static void ThrowRocketExhaustFlame(Vector3 loc, float size)
        {
            IntVec3 intVec = loc.ToIntVec3();
            if (!intVec.InBounds(Find.VisibleMap))
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDefOf.Mote_ShotFlash, null);
            moteThrown.Scale = Rand.Range(1.5f, 2.5f) * size;
            moteThrown.exactRotation = Rand.Range(-0.5f, 0.5f);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(30, 40), Rand.Range(0.08f, 0.12f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), Find.VisibleMap);
        }

        public static void ThrowSmokeForRocketsandMortars(Vector3 loc, float size)
        {
            IntVec3 intVec = loc.ToIntVec3();
            if (!intVec.InBounds(Find.VisibleMap))
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDefOf.Mote_Smoke, null);
            moteThrown.Scale = Rand.Range(1.5f, 2.5f) * size;
            moteThrown.exactRotation = Rand.Range(-0.5f, 0.5f);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(30, 40), Rand.Range(0.08f, 0.12f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), Find.VisibleMap);
        }
    }
}