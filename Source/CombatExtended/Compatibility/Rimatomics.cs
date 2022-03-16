using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.Sound;
using System.Reflection.Emit;
using System;
using UnityEngine;
using RimWorld;
using Rimatomics;


namespace CombatExtended.Compatibility
{
    class Rimatomics
    {
        public static SoundDef HitSoundDef = null;


        public static List<ThingComp> shields;

        public static int lastCacheTick = 0;
        public static Map lastCacheMap = null;

        public static bool CanInstall()
        {
	    return ModLister.HasActiveModWithName("Dubs Rimatomics");
        }
        public static void Install()
        {
	    BlockerRegistry.RegisterCheckForCollisionCallback(Rimatomics.CheckForCollisionCallback);
            BlockerRegistry.RegisterImpactSomethingCallback(Rimatomics.ImpactSomethingCallback);
        }
        public static bool CheckForCollisionCallback(ProjectileCE projectile, IntVec3 cell, Thing launcher)
        {
            Map map = projectile.Map;
            Vector3 exactPosition = projectile.ExactPosition;
            IntVec3 origin = projectile.OriginIV3;
            getShields(map);

	    if (projectile.launcher == null) {
		return false;
	    }

            foreach (ThingComp thingComp in shields)
            {
                var shield = thingComp as CompRimatomicsShield;
                if (!shield.Active || shield.ShieldState != ShieldState.Active)
                {
                    continue;
                }
                if (!GenHostility.HostileTo(projectile.launcher, shield.parent) && !shield.debugInterceptNonHostileProjectiles && !shield.Props.interceptNonHostileProjectiles)
                {
                    continue;
                }
		bool interceptOutgoing = shield.Props.interceptOutgoingProjectiles;
		
		
                int fieldRadius = (int)shield.Radius;
                int fieldRadiusSq = fieldRadius * fieldRadius;
                float DistanceSq = projectile.Position.DistanceToSquared(shield.parent.Position) - fieldRadiusSq;
                float originDistanceSq = origin.DistanceToSquared(shield.parent.Position) - fieldRadiusSq;
                if (DistanceSq > 0)
                {
                    if (!interceptOutgoing || originDistanceSq >= 0)
		    {
			continue;
		    }
                }
                else if (originDistanceSq < 0)
                {
                    continue;
                }
                Vector3 shieldPosition2D = new Vector3(shield.parent.Position.x, 0, shield.parent.Position.z);
                Quaternion targetAngle = projectile.ExactRotation;
                Quaternion shieldProjAng = Quaternion.LookRotation(exactPosition - shieldPosition2D);
		var angle = Quaternion.Angle(targetAngle, shieldProjAng);
                if (angle > 90 || (interceptOutgoing && angle < 90))
                {
                    int damage = (projectile.def.projectile.GetDamageAmount(launcher));

                    exactPosition = BlockerRegistry.GetExactPosition(origin.ToVector3(), projectile.ExactPosition, shield.parent.Position.ToVector3(), (fieldRadius - 1) * (fieldRadius - 1));
                    FleckMakerCE.ThrowLightningGlow(exactPosition, map, 0.5f);
                    projectile.ExactPosition = exactPosition;


		    Effecter effecter = new Effecter(shield.Props.interceptEffect ?? EffecterDefOf.Interceptor_BlockedProjectile);
		    effecter.Trigger(new TargetInfo(IntVec3Utility.ToIntVec3(exactPosition), shield.parent.Map, false), TargetInfo.Invalid);
		    effecter.Cleanup();
		    shield.energy -= damage * shield.EnergyLossPerDamage;
		    if (shield.energy < 0f)
		    {
			DamageInfo dinfo = new DamageInfo(projectile.def.projectile.damageDef, (float)damage, 0f, -1f, null, null, null, 0, null, true, true);
			shield.BreakShield(dinfo);
		    }



		    
                    return true;
                }
            }
            return false;
        }
        public static bool ImpactSomethingCallback(ProjectileCE projectile, Thing launcher)
        {
            bool flyOverhead = projectile.def.projectile.flyOverhead;
            if (!flyOverhead)
            {
                return false;
            }
            Map map = projectile.Map;
            getShields(map);
            Vector3 destination = projectile.ExactPosition;
            foreach (CompRimatomicsShield shield in shields)
            {
		if (!shield.Active || shield.ShieldState != ShieldState.Active)
                {
                    continue;
                }
                if (!GenHostility.HostileTo(projectile.launcher, shield.parent) && !shield.debugInterceptNonHostileProjectiles && !shield.Props.interceptNonHostileProjectiles)
                {
                    continue;
                }
                int fieldRadius = (int)shield.Radius;
                int fieldRadiusSq = fieldRadius * fieldRadius;
                float DistanceSq = projectile.Position.DistanceToSquared(shield.parent.Position) - fieldRadiusSq;
                if (DistanceSq > 0)
                {
                    continue;
                }

                int damage = (projectile.def.projectile.GetDamageAmount(launcher));
                Effecter effecter = new Effecter(shield.Props.interceptEffect ?? EffecterDefOf.Interceptor_BlockedProjectile);
		effecter.Trigger(new TargetInfo(projectile.Position, shield.parent.Map, false), TargetInfo.Invalid);
		effecter.Cleanup();
		shield.energy -= damage * shield.EnergyLossPerDamage;
		if (shield.energy < 0f)
		{
		    DamageInfo dinfo = new DamageInfo(projectile.def.projectile.damageDef, (float)damage, 0f, -1f, null, null, null, 0, null, true, true);
		    shield.BreakShield(dinfo);
		}
                return true;
            }
            return false;
        }


        public static void getShields(Map map)
        {
            int thisTick = Find.TickManager.TicksAbs;
            if (lastCacheTick != thisTick || lastCacheMap != map)
            {
                IEnumerable<Building> buildings = map.listerBuildings.allBuildingsColonist.Where(b => b is Building_ShieldArray);
		shields = new List<ThingComp>();
		foreach (Building b in buildings)
		{
		    shields.Add((b as Building_ShieldArray).CompShield);
		}
		
		lastCacheTick = thisTick;
                lastCacheMap = map;
            }
        }

    }
}



