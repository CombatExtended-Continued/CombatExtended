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

                var toFixScenarios = DefDatabase<ScenarioDef>.AllDefs.Where(x => x.scenario.AllParts.Any(y => y is ScenPart_ScatterThings || y is ScenPart_StartingThing_Defined));
                foreach (ScenarioDef def in toFixScenarios)
                {
                    var PartAmmos = def.scenario.AllParts.Where(y => y is ScenPart_StartingThing_Defined
                                    && ((ScenPart_StartingThing_Defined)y).thingDef is AmmoDef).Select(x => x as ScenPart_StartingThing_Defined);

                    foreach (ScenPart_StartingThing_Defined scene in PartAmmos)
                    {
                        var ammodef = ((AmmoDef)scene.thingDef);

                        var ammoreplaced = ammodef.AmmoSetDefs?.FirstOrFallback()?.ammoTypes?.Find(x => x.ammo.ammoClass == ammodef.ammoClass)?.ammo ?? null;

                        if (ammoreplaced != null)
                        {
                            scene.thingDef = ammoreplaced;
                        }


                    }
                }

                var toFixComps = DefDatabase<ThingDef>.AllDefs.Where(x => x.comps?.Any(x => x is CompProperties_ApparelReloadable) ?? false);

                foreach (var def in toFixComps)
                {

                    var compProps = def.GetCompProperties<CompProperties_ApparelReloadable>();
                    if (compProps.ammoDef is AmmoDef am)
                    {
                        if (am.AmmoSetDefs.NullOrEmpty())
                        {
                            Log.Warning($"Apparel {def} has CE AmmoDef but with no AmmoSetDef");
                            continue;
                        }

                        //I don't know why this works but it does. It seems like the AmmoSetDefs here only contain the generic ammo version.
                        var ammoset = am.AmmoSetDefs.Find(x => x.similarTo == null);
                        if (ammoset != null)
                        {
                            Log.Message(def.label + " switching to " + ammoset.label);
                            var ammo = ammoset.ammoTypes.Find(x => (x.ammo?.ammoClass ?? null) == am.ammoClass)?.ammo ?? null;

                            if (ammo == null)
                            {
                                ammo = ammoset.ammoTypes[0].ammo;
                            }
                            compProps.ammoDef = ammo;
                        }
                        else if (!am.AmmoSetDefs.NullOrEmpty())
                        {
                            Log.Warning($"Apparel {def} has CE AmmoDef {am.AmmoSetDefs[0]} but no similarTo tag");
                        }
                        else
                        {
                            Log.Warning($"Apparel {def} has CE AmmoDef with no AmmoSetDefs");
                        }
                    }
                }
            }


        }
    }
}
