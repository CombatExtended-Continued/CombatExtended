using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using RimWorld.Planet;
using CombatExtended.Loader;

namespace CombatExtended.Compatibility.VehiclesCompat
{
    public class VehicleSettings : ISettingsCE
    {
	public VehicleSettings()
	{
	}
	public void DoWindowContents(Listing_Standard list)
        {
	    Text.Font = GameFont.Medium;
            list.Label("CE_Settings_Vehicles".Translate());
            Text.Font = GameFont.Small;
            list.Gap();
	    list.CheckboxLabeled("PatchArmorDamage".Translate(), ref Controller.settings.patchArmorDamage, "PatchArmorDamage".Translate());
	}

    }
}
