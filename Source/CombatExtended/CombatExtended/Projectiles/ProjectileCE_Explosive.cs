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
        public override void Impact(Thing hitThing)
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
    }
}
