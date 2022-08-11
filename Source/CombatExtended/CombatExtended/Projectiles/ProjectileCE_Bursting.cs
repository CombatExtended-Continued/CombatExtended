using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using CombatExtended.Compatibility;
using CombatExtended.Lasers;
using ProjectileImpactFX;
using CombatExtended.Utilities;

namespace CombatExtended
{
    class ProjectileCE_Bursting : ProjectileCE
    {
        private int ticksToBurst;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.ticksToBurst, "ticksToBurst", -1, false);
        }

        public override void Launch(Thing launcher, Vector2 origin, float shotAngle, float shotRotation, float shotHeight = 0f, float shotSpeed = -1f, Thing equipment = null, float distance = -1)
        {
            if (distance > 0)
            {
                float cosine = (float)Math.Cos((double)shotAngle);

                float fuzeTiming = distance / ((shotSpeed / 60) * cosine);
                //Log.Message("Distance    = " + distance);
                //Log.Message("ShotSpeed   = " + shotSpeed / 60);
                //Log.Message("Cosine      = " + cosine);
                // Log.Message("Fuse timing = " + fuzeTiming);
                //Log.Message("Launched ProjectileCEBursting with ticks to burst = " + fuzeTiming);

                ticksToBurst = (int)fuzeTiming;
            }

            this.shotAngle = shotAngle;
            this.shotHeight = shotHeight;
            this.shotRotation = shotRotation;
            this.shotSpeed = Math.Max(shotSpeed, def.projectile.speed);
            Launch(launcher, origin, equipment);
            this.ticksToImpact = IntTicksToImpact;
        }

        public override void Launch(Thing launcher, Vector2 origin, Thing equipment = null)
        {
            base.Launch(launcher, origin, equipment);
        }

        public override void Tick()
        {
            base.Tick();
            if (this.ticksToBurst >= 0)
            {
                this.ticksToBurst--;
                if (this.ticksToBurst <= 0)
                {
                    //Explosions are all handled in base
                    base.Impact(null);
                }
            }
        }
    }
}
