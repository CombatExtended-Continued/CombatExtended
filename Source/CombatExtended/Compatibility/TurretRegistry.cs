using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using CombatExtended.CombatExtended.LoggerUtils;
using CombatExtended.CombatExtended.Jobs.Utils;
using Verse.AI;

namespace CombatExtended.Compatibility
{
    using GetReloadingFunction = Func<Building_Turret,bool>;
    using SetReloadingFunction = Action<Building_Turret,bool>;

    using GetAmmoFunction   = Func<Building_Turret,CompAmmoUser>;

    using GetGunFunction    = Func<Building_Turret,Thing>;

    public static class TurretRegistry
    {
	private static bool enabled = false;
	private static Dictionary<Type, SetReloadingFunction> setReloading;
	private static Dictionary<Type, GetReloadingFunction> getReloading;
	private static Dictionary<Type, GetAmmoFunction> getAmmo;
	private static Dictionary<Type, GetGunFunction> getGun;
	
        private static void Enable()
        {
            enabled = true;
	    setReloading = new Dictionary<Type, SetReloadingFunction>();
	    getReloading = new Dictionary<Type, GetReloadingFunction>();
	    getAmmo      = new Dictionary<Type, GetAmmoFunction>();
	    getGun       = new Dictionary<Type, GetGunFunction>();
	    
        }

        public static void RegisterReloadableTurret(Type turretType, SetReloadingFunction setReload, GetReloadingFunction getReload, GetGunFunction gun, GetAmmoFunction ammo=null)
        {
            if (!enabled)
            {
                Enable();
            }
	    getReloading[turretType] = getReload;
	    setReloading[turretType] = setReload;

	    getAmmo[turretType] = ammo;
	    getGun[turretType] = gun;
        }

	public static void SetReloading(this Building_Turret turret, bool reloading) {
	    if (turret is Building_TurretGunCE ceturret) {
		ceturret.isReloading = reloading;
		return;
	    }
	    if (enabled) {
		SetReloadingFunction func;
		if (setReloading.TryGetValue(turret.GetType(), out func)) {
		    func(turret, reloading);
		    return;
		}
	    }
	    CELogger.Warn("Asked to set reloading on an unknown turret type: "+turret);
	}

	public static bool GetReloading(this Building_Turret turret) {
	    if (turret is Building_TurretGunCE ceturret) {
		return ceturret.isReloading;
	    }
	    if (enabled) {
		GetReloadingFunction func;
		if (getReloading.TryGetValue(turret.GetType(), out func)) {
		    return func(turret);
		}
	    }
	    CELogger.Warn("Asked to get reloading on an unknown turret type: "+turret);
	    return false;
	}

	public static CompAmmoUser GetAmmo(this Building_Turret turret) {
	    if (turret is Building_TurretGunCE ceturret) {
		return ceturret.CompAmmo;
	    }
	    if (enabled) {
		GetAmmoFunction func;
		if (getAmmo.TryGetValue(turret.GetType(), out func)) {
		    return func(turret);
		}
		GetGunFunction gfunc;
		if (getGun.TryGetValue(turret.GetType(), out gfunc)) {
		    return gfunc(turret)?.TryGetComp<CompAmmoUser>();
		}
		
	    }
	    CELogger.Warn("Asked to get ammo on an unknown turret type: "+turret);
	    return null;
	}

	public static Thing GetGun(this Building_Turret turret) {
	    if (turret is Building_TurretGunCE ceturret) {
		return ceturret.Gun;
	    }
	    if (enabled) {
		GetGunFunction gfunc;
		if (getGun.TryGetValue(turret.GetType(), out gfunc)) {
		    return gfunc(turret);
		}
		
	    }
	    CELogger.Warn("Asked to get gun on an unknown turret type: "+turret);
	    return null;
	}

	public static CompMannable GetMannable(this Building_Turret turret) {
	    if (turret is Building_TurretGunCE ceturret) {
		return ceturret.MannableComp;
	    }
	    return turret.TryGetComp<CompMannable>();
	}

	public static bool GetReloadable(this Building_Turret turret) {
	    return turret.GetAmmo()?.HasMagazine ?? false;
	}
	
	public static bool ShouldReload(this Building_Turret turret, float threshold=0.5f, bool ensureAmmoType=true) {
	    var ammo = turret.GetAmmo();
	    if (ammo==null) {
		return false;
	    }
	    return (ammo.HasMagazine && ammo.CurMagCount <= ammo.MagSize * threshold) || (ensureAmmoType && ammo.CurrentAmmo != ammo.SelectedAmmo);
	}

	public static void TryForceReload(this Building_Turret turret) {
	    turret.TryOrderReload(true);
	}

	public static void TryOrderReload(this Building_Turret turret, bool forced = false)
        {
	    if (turret is Building_TurretGunCE ceturret) {
		ceturret.TryOrderReload(forced);;
		return;
	    }
	    var compAmmo = turret.GetAmmo();
            if ((compAmmo.CurrentAmmo == compAmmo.SelectedAmmo && (!compAmmo.HasMagazine || compAmmo.CurMagCount == compAmmo.MagSize)))
                return;
	    var mannableComp = turret.GetMannable();
            if (!mannableComp?.MannedNow ?? true)
            {
                return;
            }

            //Only have manningPawn reload after a long time of no firing
            if (!forced && compAmmo.CurMagCount != 0)
                return;

            //Already reserved for manning
            Pawn manningPawn = mannableComp.ManningPawn;
            if (manningPawn != null)
            {
                if (!JobGiverUtils_Reload.CanReload(manningPawn, turret))
                {
                    return;
                }
                var jobOnThing = JobGiverUtils_Reload.MakeReloadJob(manningPawn, turret);

                if (jobOnThing != null)
                {
                    manningPawn.jobs.StartJob(jobOnThing, JobCondition.Ongoing, null, manningPawn.CurJob?.def != CE_JobDefOf.ReloadTurret);
                }
            }

        }


	
    }
}
