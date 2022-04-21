using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Verse;
using CombatExtended.Loader;
using RimWorld;
using System.Collections.Generic;

namespace CombatExtended.Compatibility
{
    public class MiscTurrets: IPatch
    {
	public bool CanInstall() {
	    Log.Message("Combat Extended :: Checking Misc Turrets");
	    return ModLister.HasActiveModWithName("Misc. TurretBase, Objects");
	}

	public void Install() {
	    Log.Message("Combat Extended :: Installing Misc Turrets");
	}

	public IEnumerable<string> GetCompatList() {
	    yield return "MiscTurretsCompat";
	}



    }
}
