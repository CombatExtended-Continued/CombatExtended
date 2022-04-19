using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Verse;
using CombatExtended.Loader;
using RimWorld;

namespace CombatExtended.Compatibility
{
    public class BetterTurrets
    {
	public static bool CanInstall() {
	    Log.Message("Combat Extended :: Checking Better Turrets");
	    return ModLister.HasActiveModWithName("Misc Turret Base Rearmed");
	}

	public static void Install() {
	    Log.Message("Combat Extended :: Installing Better Turrets");
	    Loader.Loader.LoadCompatAssembly("BetterTurretsCompat");
	}



    }
}
