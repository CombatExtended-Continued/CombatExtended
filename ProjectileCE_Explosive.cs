using System;
using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;

namespace CombatExtended
{
    //Cloned from vanilla, completely unmodified
    public class ProjectileCE_Explosive : ProjectileCE
    {
        private int ticksToDetonation;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.ticksToDetonation, "ticksToDetonation", 0, false);
        }
        public override void Tick()
        {
            base.Tick();
            if (this.ticksToDetonation > 0)
            {
                this.ticksToDetonation--;
                if (this.ticksToDetonation <= 0)
                {
					//Explosions are all handled in base
                    base.Impact(null);
                }
            }
        }
        protected override void Impact(Thing hitThing)
        {
            // Snap to target so we hit multi-tile pawns with our explosion
            if (hitThing is Pawn)
            {
                var newPos = hitThing.DrawPos;
                newPos.y = ExactPosition.y;
                ExactPosition = newPos;
                Position = ExactPosition.ToIntVec3();
            }
            if (def.projectile.explosionDelay == 0)
            {
				//Explosions are all handled in base
                base.Impact(null);
                return;
            }
            landed = true;
            ticksToDetonation = def.projectile.explosionDelay;
            GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(this, this.def.projectile.damageDef, this.launcher?.Faction);
        }
            //This code was disabled because it didn't run under previous circumstances. Could be enabled if necessary
            /*
            if (map != null && base.ExactPosition.ToIntVec3().IsValid)
            {
                ThrowBigExplode(base.ExactPosition + Gen.RandomHorizontalVector(def.projectile.explosionRadius * 0.5f), base.Map, def.projectile.explosionRadius * 0.4f);
            }
            */
        /*public static void ThrowBigExplode(Vector3 loc, Map map, float size)
          {
              if (!loc.ShouldSpawnMotesAt(map))
              {
                  return;
              }
              MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDef.Named("Mote_BigExplode"), null);
              moteThrown.Scale = Rand.Range(5f, 6f) * size;
              moteThrown.exactRotation = Rand.Range(0f, 0f);
              moteThrown.exactPosition = loc;
              moteThrown.SetVelocity((float)Rand.Range(6, 8), Rand.Range(0.002f, 0.003f));
              GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
          }*/
    }
}
