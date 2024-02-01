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
    class ProjectileCE_HeightFuse : ProjectileCE
    {
        float detonationHeight => (def.projectile as ProjectilePropertiesCE).aimHeightOffset;

        bool armed;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref armed, "armed", false);
        }

        public override void Tick()
        {
            base.Tick();
            if (!armed && LastPos.y > detonationHeight)
            {
                armed = true;
            }
            if (armed && Height <= detonationHeight)
            {
                HeightFuseAirBurst();
            }
        }

        public override void Impact(Thing hitThing)
        {
            //intercept impact if it hit something after where height fuse should have triggered
            if (armed && Height <= detonationHeight)
            {
                HeightFuseAirBurst();
            }
            else
            {
                base.Impact(hitThing);
            }
        }

        void HeightFuseAirBurst()
        {
            float f = (LastPos.y - detonationHeight) / (LastPos.y - Height);
            ExactPosition = f * (LastPos - ExactPosition);
            if (!ExactPosition.ToIntVec3().IsValid)
            {
                Destroy();
                return;
            }
            base.Impact(null);
        }
    }
}
