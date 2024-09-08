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

        public override void Launch(Thing launcher, Vector2 origin, float shotAngle, float shotRotation, float shotHeight = 0f, float shotSpeed = -1f, Thing equipment = null, float distance = -1, int ticksToTruePosition = 3)
        {
            int armingDelay = 0;
            if (def.projectile is ProjectilePropertiesCE props)
            {
                armingDelay = props.armingDelay;
                this.castShadow = props.castShadow;
            }
            if (distance > 0)
            {
                float cosine = (float)Math.Cos((double)shotAngle);

                float fuzeTiming = distance / ((shotSpeed / 60) * cosine);
#if DEBUG
                Log.Message("Distance    = " + distance);
                Log.Message("ShotSpeed   = " + shotSpeed / 60);
                Log.Message("Cosine      = " + cosine);
                Log.Message("Fuse timing = " + fuzeTiming);
                Log.Message("Launched ProjectileCEBursting with ticks to burst = " + fuzeTiming);
#endif

                ticksToBurst = (int)fuzeTiming;
            }
            if (ticksToBurst < armingDelay)
            {
                ticksToBurst = armingDelay;
            }

            this.shotAngle = shotAngle;
            this.shotHeight = shotHeight;
            this.shotRotation = shotRotation;
            this.shotSpeed = Math.Max(shotSpeed, def.projectile.speed);
            Launch(launcher, origin, equipment);
        }

        public override void Tick()
        {
            base.Tick();
            if (--this.ticksToBurst == 0)
            {
                base.Impact(null);
            }
        }
    }
}
