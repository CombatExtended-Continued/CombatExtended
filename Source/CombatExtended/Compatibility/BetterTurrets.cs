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
    public class BetterTurrets: IPatch
    {
	public bool CanInstall() {
	    Log.Message("Combat Extended :: Checking Better Turrets");
	    return ModLister.HasActiveModWithName("Misc Turret Base Rearmed");
	}

	public void Install() {
	    Log.Message("Combat Extended :: Installing Better Turrets");
	}
	public IEnumerable<string> GetCompatList() {
	    yield return "BetterTurretsCompat";
	}



    }
}
