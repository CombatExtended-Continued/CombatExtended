using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Verse;
using CombatExtended.Loader;
using RimWorld;
using System.Collections.Generic;
using HarmonyLib;
using Vehicles;
using UnityEngine;


namespace CombatExtended.Compatibility.VehiclesCompat {
    [StaticConstructorOnStartup]
    public class VehiclesCompat: IModPart {
        public Type GetSettingsType() {
            return null;
        }
        public IEnumerable<string> GetCompatList() {
            yield break;
        }
        public void PostLoad(ModContentPack content, ISettingsCE _) {
	    VehicleTurret.ProjectileAngleCE = ProjectileCE.GetShotAngle;
	    VehicleTurret.LaunchProjectileCE = LaunchProjectileCE;
	    global::CombatExtended.Compatibility.Vehicles.CollisionBodyFactorCallbacks.Add(_GetCollisionBodyFactors);
        }


	public static object LaunchProjectileCE(ThingDef projectileDef,
						Vector2 origin,
						LocalTargetInfo target,
						VehiclePawn vehicle,
						float shotAngle,
						float shotRotation,
						float shotHeight,
						float shotSpeed)
	{
	    projectileDef = projectileDef.GetProjectile();
	    ProjectileCE projectile = (ProjectileCE)ThingMaker.MakeThing(projectileDef, null);
	    GenSpawn.Spawn(projectile, vehicle.Position, vehicle.Map);

	    projectile.ExactPosition = origin;
	    projectile.canTargetSelf = false;
	    projectile.minCollisionDistance = 1;
	    projectile.intendedTarget = target;
	    projectile.mount = null;
	    projectile.AccuracyFactor = 1;

	    
	    projectile.Launch(
			      vehicle,
			      origin,
			      shotAngle,
			      shotRotation,
			      shotHeight,
			      shotSpeed,
			      vehicle);
	    return projectile;
	}
	private static Tuple<bool,Vector2> _GetCollisionBodyFactors(Pawn pawn) {
	    Vector2 ret = new Vector2();
	    if (pawn is VehiclePawn vehicle) {
		ret = new Vector2(1, vehicle.def.fillPercent);
		return new Tuple<bool,Vector2>(true, ret);
	    }
	    return new Tuple<bool, Vector2>(false, ret);
	}

    }
}
