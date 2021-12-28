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
            /* 
             * Generic ammosetdefs need projectiles for the code to work, but what the projectiles are doesn't matter
             * Also generic ammosetdefs need their own recipes for ammo, the similarTo caliber's will have their recipes hidden
             */
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

                        var makeRecipe = DefDatabase<RecipeDef>.AllDefs.Where(x => x.defName == "Make" + link.ammo.defName).FirstOrFallback();

                        link.ammo.SetMenuHidden(true);

                        if (makeRecipe != null)
                        {
                            if (CE_ThingDefOf.AmmoBench.AllRecipes.Contains(makeRecipe))
                            {
                                CE_ThingDefOf.AmmoBench.AllRecipes.Remove(makeRecipe);
                            }

                            if (makeRecipe.AllRecipeUsers.Count() > 0 | (makeRecipe.recipeUsers?.Count() ?? 0) > 0)
                            {
                                makeRecipe.recipeUsers = new List<ThingDef>();
                            }
                        }
                        

                        
                    }
                    amset.label = ammoSource.label;
                    amset.ammoTypes = newAmmos;

                }
            }

           
        }
    }
}
