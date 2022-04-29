using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;

using CombatExtended;
using CombatExtended.Compatibility;
namespace CombatExtended.Compatibility {
    
    public class MiscTurrets_CE_TurretWeaponBase : TurretWeaponBase.Building_TurretWeaponBase {
	private bool _reloading = false;
	private CompAmmoUser _ammo;
	private CompFireModes compFireModes = null;
	public CompAmmoUser GetAmmo() {
	    Thing gun = GetGun();
	    if (gun == null) {
		_ammo = null;
		return null;
	    }
	    if (_ammo == null) {
		_ammo = gun.TryGetComp<CompAmmoUser>();
		_ammo.turret = this;
	    }
	    return _ammo;
	}

	public Thing GetGun() {
	    return gun;
	}
	
	public void SetReload(bool reloading) {
	    _reloading = reloading;
	}
	
	public bool GetReload() {
	    return _reloading;
	}

	public CompFireModes CompFireModes
	{
	    get
	    {
		if (compFireModes == null && gun != null) compFireModes = gun.TryGetComp<CompFireModes>();
		return compFireModes;
	    }
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad) {
	    base.SpawnSetup(map, respawningAfterLoad);
	    Map.GetComponent<TurretTracker>().Register(this);

	}
	public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish) {
	    Map.GetComponent<TurretTracker>().Unregister(this);
            base.DeSpawn(mode);

	}
	public override void Tick() {
	    if (_reloading) {
		return;
	    }
	    base.Tick();
	}

	public override IEnumerable<Gizmo> GetGizmos() {
	    foreach (var c in base.GetGizmos())
		yield return c;
	    var ammo = GetAmmo();
	    if (ammo!=null) {
		foreach (Command com in ammo.CompGetGizmosExtra())
		{
		    if (base.Faction != Faction.OfPlayer && Prefs.DevMode && com is GizmoAmmoStatus)
			(com as GizmoAmmoStatus).prefix = "DEV: ";
		  
		    yield return com;
		}
	      
	    }
	    if (gun!=null) {
		if (CompFireModes != null)
                {
                    foreach (Command com in CompFireModes.GenerateGizmos())
                    {
                        yield return com;
                    }
                }

	    }

	}
	

    }
}
