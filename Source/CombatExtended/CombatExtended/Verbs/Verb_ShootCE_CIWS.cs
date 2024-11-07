﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class Verb_ShootCE_CIWS : Verb_ShootCE
    {
        public Building_CIWS_CE Turret => Caster as Building_CIWS_CE;
        public override ThingDef Projectile
        {
            get
            {
                var result = base.Projectile;
                var ciwsVersion = (result?.projectile as ProjectilePropertiesCE)?.CIWSVersion;
                if (ciwsVersion == null && !typeof(ProjectileCE_CIWS).IsAssignableFrom(result.thingClass))
                {
                    Log.WarningOnce($"{result} is not a CIWS projectile and the projectile does not have the CIWS version specified in its properties. Must be on-ground projectile used for CIWS", result.GetHashCode());
                }
                return ciwsVersion ?? result;
            }
        }

        protected int maximumPredectionTicks = 40;
        public override bool TryFindCEShootLineFromTo(IntVec3 root, LocalTargetInfo targ, out ShootLine resultingLine)
        {
            resultingLine = default;
            return false;
        }

    }
}