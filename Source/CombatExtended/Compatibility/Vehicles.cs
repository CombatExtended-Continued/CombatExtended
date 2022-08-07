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
using Vehicles;


namespace CombatExtended.Compatibility
{
    class Vehicles: IPatch
    {
	public bool CanInstall()
        {
            if (!ModLister.HasActiveModWithName("Vehicle Framework"))
            {
                return false;
            }
            return true;
        }
	public IEnumerable<string> GetCompatList() {
	    yield break;
	}

	public void Install()
        {
	    VehicleTurret.ProjectileAngleCE = ProjectileCE.GetShotAngle;
	    VehicleTurret.LaunchProjectileCE = LaunchProjectileCE;
	}
	public static object LaunchProjectileCE(ThingDef projectileDef,
						Vector3 origin,
						VehiclePawn vehicle,
						float shotAngle,
						float shotRotation,
						float shotHeight,
						float shotSpeed)
	{
	    projectileDef = projectileDef.GetProjectile();
	    ProjectileCE projectile = (ProjectileCE)ThingMaker.MakeThing(projectileDef, null);
	    GenSpawn.Spawn(projectile, origin.ToIntVec3(), vehicle.Map);
	    projectile.Launch(
			      vehicle,    //Shooter instead of caster to give turret operators' records the damage/kills obtained
			      origin,
			      shotAngle,
			      shotRotation,
			      shotHeight,
			      shotSpeed,
			      vehicle);
	    return projectile;
	}

    }
}
