using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Verse;
using CombatExtended.Loader;
using RimWorld;
using System.Collections.Generic;

namespace CombatExtended.Compatibility {
    public class BetterTurretsCompat: IModPart {
	public Type GetSettingsType() {
	    return null;
	}
	public IEnumerable<string> GetCompatList() {
	    yield return "BetterTurretsCompat";
	}
	public void PostLoad(ModContentPack content, ISettingsCE _) {
	    TurretRegistry.RegisterReloadableTurret(
						    typeof(MiscTurrets_CE_TurretWeaponBase),
						    ((Building_Turret t, bool r) => {(t as MiscTurrets_CE_TurretWeaponBase).SetReload(r);}),
						    ((Building_Turret t) => {return (t as MiscTurrets_CE_TurretWeaponBase).GetReload();}),
						    ((Building_Turret t) => {return (t as MiscTurrets_CE_TurretWeaponBase).GetGun();}),
						    ((Building_Turret t) => {return (t as MiscTurrets_CE_TurretWeaponBase).GetAmmo();}));
	}
    }
}
