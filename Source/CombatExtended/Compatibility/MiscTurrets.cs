using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Verse;
using CombatExtended.Loader;
using RimWorld;

namespace CombatExtended.Compatibility
{
    public class MiscTurrets
    {
	public static bool CanInstall() {
	    Log.Message("Combat Extended :: Checking Misc Turrets");
	    return ModLister.HasActiveModWithName("Misc. TurretBase, Objects");
	}

	public static void Install() {
	    Log.Message("Combat Extended :: Installing Misc Turrets");
	    Loader.Loader.LoadCompatAssembly("MiscTurretsCompat");
	}



    }
}
