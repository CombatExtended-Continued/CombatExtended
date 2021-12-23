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

                foreach (AmmoSetDef amset in toGenericAmmos)
                {
                    List<AmmoLink> newAmmos = new List<AmmoLink>();
                    var ammoSource = amset.similarTo;
                    foreach (AmmoLink link in amset.ammoTypes)
                    {
                        var sameClass = ammoSource.ammoTypes.Find(x => x.ammo.ammoClass == link.ammo.ammoClass);
                        if (sameClass != null)
                        {
                            link.projectile.label = ammoSource.label + " bullet " + "(" + link.ammo.ammoClass.labelShort + ")";
                            newAmmos.Add(new AmmoLink { ammo = sameClass.ammo, projectile = link.projectile });
                        }


                        

                        
                    }

                    amset.label = ammoSource.label;
                    amset.ammoTypes = newAmmos;

                }

                /*
                var GunsToRedo = DefDatabase<ThingDef>.AllDefs.Where(x => 
                x.comps.Any(y => y is CompProperties_AmmoUser)
                && ((CompProperties_AmmoUser)x.comps.Find(y => y is CompProperties_AmmoUser)).ammoSet.similarTo != null
                );

                foreach (ThingDef gun in GunsToRedo)
                {
                    var CompAmmo = (CompProperties_AmmoUser)gun.comps.Find(x => x is CompProperties_AmmoUser);

                    var GenCaliber = CompAmmo.ammoSet.similarTo;

                    CompAmmo.ammoSet = GenCaliber;


                }*/
            }

           
        }
    }
}
