using Verse;
using ProjectRimFactory.Industry;
using System;
using System.Collections.Generic;

namespace CombatExtended.Compatibility
{
    public class ProjectRimFactoryCompat: IPatch
    {
        public bool CanInstall()
        {
            return ModLister.GetActiveModWithIdentifier("spdskatr.projectrimfactory") != null;
        }

        public void Install()
        {
            Building_FuelingMachine.RegisterRefuelable(typeof(Building_TurretGunCE), FindCompAmmoUser, TestAmmo, ReloadAction);
        }
	public IEnumerable<string> GetCompatList() {
	    yield break;
	}

        private static int TestAmmo(object compObject, Thing ammo)
        {
            var comp = compObject as CompAmmoUser;
            // Not the correct ammo type (I'm looking at FMJ, but turret asks for AP)
            if (ammo.def != comp.SelectedAmmo) { return 0; }
            return Math.Min(comp.MissingToFullMagazine, ammo.stackCount);
        }

        private static void ReloadAction(object compObject, Thing ammo)
        {
            var comp = compObject as CompAmmoUser;
            if (ammo.def != comp.CurrentAmmo) { comp.TryUnload(); }
            comp.LoadAmmo(ammo);
        }

        static object FindCompAmmoUser(Building building)
        {
            var compAmmoUser = (building as Building_TurretGunCE).CompAmmo;
            if (!compAmmoUser.FullMagazine) return compAmmoUser;
            return null;
        }
    }
}
