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

                        link.ammo.AmmoSetDefs.Add(ammoSource);
                        

                        
                    }

                    amset.label = ammoSource.label;
                    amset.ammoTypes = newAmmos;

                }

                var toFixScenarios = DefDatabase<ScenarioDef>.AllDefs.Where(x => x.scenario.AllParts.Any(y => y.def == ScenPartDefOf.ScatterThingsAnywhere | y.def == ScenPartDefOf.ScatterThingsNearPlayerStart | y.def == ScenPartDefOf.StartingThing_Defined));
                foreach(ScenarioDef def in toFixScenarios)
                {
                    var PartAmmos = def.scenario.AllParts.Where(y => y is ScenPart_StartingThing_Defined
                    && ((ScenPart_StartingThing_Defined)y).thingDef is AmmoDef).Select(x => x as ScenPart_StartingThing_Defined);

                    foreach (ScenPart_StartingThing_Defined scene in PartAmmos)
                    {
                        var ammodef = ((AmmoDef)scene.thingDef);

                        var ammoreplaced = ammodef.AmmoSetDefs?.FirstOrFallback()?.ammoTypes?.Find(x => x.ammo.ammoClass == ammodef.ammoClass)?.ammo ?? null;

                        if(ammoreplaced != null)
                        {
                            scene.thingDef = ammoreplaced;
                        }

                        
                    }
                }

                var toFixComps = DefDatabase<ThingDef>.AllDefs.Where(x => x.comps?.Any(x => x is CompProperties_Reloadable) ?? false);

                foreach (var def in toFixComps)
                {
                    
                    var compProps = def.GetCompProperties<CompProperties_Reloadable>();
                    if (compProps.ammoDef is AmmoDef am)
                    {
                        //95% of the time there is only one ammoset, esspecially for armor shotties
                        var ammoset = am.AmmoSetDefs.Find(x => x.similarTo != null && x.ammoTypes.Any(x => x.ammo.ammoClass == am.ammoClass));
                        if (ammoset != null)
                        {
                            compProps.ammoDef = ammoset.similarTo.ammoTypes.Find(x => x.ammo.ammoClass == am.ammoClass).ammo;
                        }
                        else if (am.AmmoSetDefs.Any(x => x.similarTo != null))
                        {
                            compProps.ammoDef = am.AmmoSetDefs.Find(x => x.similarTo != null).ammoTypes[0].ammo;
                        }
                        else
                        {
                            Log.Warning($"Apparel {def} has CE AmmoDef {am.AmmoSetDefs[0]} but no similarTo tag");
                        }
                    }
                }
            }

           
        }
    }
}
