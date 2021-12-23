using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    [StaticConstructorOnStartup]
    public class AmmoGeneralizer
    {
        static AmmoGeneralizer()
        {
            if (Controller.settings.GenericAmmo)
            {

                var toGenericAmmos = DefDatabase<AmmoSetDef>.AllDefs.Where(x => x.similarTo != null);

                foreach (AmmoSetDef togenericammo in toGenericAmmos)
                {
                    var RemadeAmmos = new List<AmmoLink>();

                    var TargetCaliber = togenericammo.similarTo;

                    foreach (AmmoLink amlink in togenericammo.ammoTypes)
                    {
                        var proj = amlink.projectile;

                        var am = amlink.ammo;

                        am.label = TargetCaliber.label + " cartridge (" + am.ammoClass.labelShort + ")";

                        var GenericProjectileOfClass = (TargetCaliber.ammoTypes.Find(M => M.ammo.ammoClass == am.ammoClass)).projectile;

                        var GenericAmType = new AmmoLink { ammo = am, projectile = GenericProjectileOfClass };

                        am.SetMenuHidden(true);

                        am.menuHidden = true;

                        RemadeAmmos.Add(GenericAmType);
                    }

                    togenericammo.similarTo.ammoTypes.AddRange(RemadeAmmos);
                }

                var GunsToRedo = DefDatabase<ThingDef>.AllDefs.Where(x => 
                x.comps.Any(y => y is CompProperties_AmmoUser)
                && ((CompProperties_AmmoUser)x.comps.Find(y => y is CompProperties_AmmoUser)).ammoSet.similarTo != null
                );

                foreach (ThingDef gun in GunsToRedo)
                {
                    var CompAmmo = (CompProperties_AmmoUser)gun.comps.Find(x => x is CompProperties_AmmoUser);

                    var GenCaliber = CompAmmo.ammoSet.similarTo;

                    CompAmmo.ammoSet = GenCaliber;


                }
            }

           
        }
    }
}
