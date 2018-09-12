using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    /* Ammo Injection Handler
     * 
     * Automatically enables all ammo types defined and in use by at least one gun somewhere. Also enables appropriate crafting recipes and trading tags.
     * This way we can have all ammo defs in one core mod and selectively enable the ones we need based on which gun mods are installed, avoiding duplication
     * issues where multiple gun mods are adding the same ammo.
     * 
     * When ammo system is disabled automatically disables crafting and spawning of all ammo. 
     * 
     * Call Inject() on game start and whenever ammo system setting is changed.
     */
    internal static class AmmoInjector
    {
        public static readonly FieldInfo _allRecipesCached = typeof(ThingDef).GetField("allRecipesCached", BindingFlags.Instance | BindingFlags.NonPublic);
    	
        private const string enableTradeTag = "CE_AutoEnableTrade";             // The trade tag which designates ammo defs for being automatically switched to Tradeability.Stockable
        private const string enableCraftingTag = "CE_AutoEnableCrafting";        // The trade tag which designates ammo defs for having their crafting recipes automatically added to the crafting table
        /*
        private static ThingDef ammoCraftingStationInt;                         // The crafting station to which ammo will be automatically added
        private static ThingDef AmmoCraftingStation
        {
            get
            {
                if (ammoCraftingStationInt == null)
                    ammoCraftingStationInt = ThingDef.Named("AmmoBench");
                return ammoCraftingStationInt;
            }
        }
        */

        public static void Inject()
        {
            if (InjectAmmos())
            {
            	Log.Message("Combat Extended :: Ammo " + (Controller.settings.EnableAmmoSystem ? "injected" : "removed"));
            }
            else
            {
            	Log.Error("Combat Extended :: Ammo injector failed to get injected");
            }
        }

        public static bool InjectAmmos()
        {
        	bool enabled = Controller.settings.EnableAmmoSystem;
            if (enabled)
            {
            	// Initialize list of all weapons
            	CE_Utility.allWeaponDefs.Clear();
            	
	            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
	            {
	            	if (def.IsWeapon
	            	    && (def.generateAllowChance <= 0
	            	        || def.tradeability == Tradeability.Buyable
	            	        || (def.weaponTags != null && def.weaponTags.Contains("TurretGun"))))
	                    CE_Utility.allWeaponDefs.Add(def);
	            }
	            if (CE_Utility.allWeaponDefs.NullOrEmpty())
	            {
	                Log.Warning("CE Ammo Injector found no weapon defs");
	                return true;
	            }
            }
            else
            {
        		//If the ammo system is not enabled and it appears that there are no weaponDefs at all ..
            	if (CE_Utility.allWeaponDefs.NullOrEmpty())
            	{
            		//.. return out of the method early because nothing has to be reversed ..
            		return true;
            	}
            	//.. else, continue the method.
            }
            
            var ammoDefs = new HashSet<ThingDef>();
            
            // Find all ammo using guns
            foreach (ThingDef weaponDef in CE_Utility.allWeaponDefs)
            {
                CompProperties_AmmoUser props = weaponDef.GetCompProperties<CompProperties_AmmoUser>();
                if (props != null && props.ammoSet != null && !props.ammoSet.ammoTypes.NullOrEmpty())
                {
                    ammoDefs.UnionWith(props.ammoSet.ammoTypes.Select<AmmoLink, ThingDef>(x => x.ammo));
                }
            }
            
            // Make sure to exclude all ammo things which double as weapons
			ammoDefs.RemoveWhere(x => x.IsWeapon);
            
            /*
            bool canCraft = (AmmoCraftingStation != null);
            
            if (!canCraft)
            {
            	Log.ErrorOnce("CE ammo injector crafting station is null", 84653201);
            }
            */
            
            foreach (AmmoDef ammoDef in ammoDefs)
            {
            	// Toggle ammo visibility in the debug menu
                ammoDef.menuHidden = !enabled;
                ammoDef.destroyOnDrop = !enabled;
                
                // Toggle trading
                if (ammoDef.tradeTags.Contains(enableTradeTag))
                {
                	ammoDef.tradeability = enabled ? Tradeability.Buyable : Tradeability.Sellable;
                }

                // Toggle craftability
                var craftingTags = ammoDef.tradeTags.Where(t => t.StartsWith(enableCraftingTag));
                if (craftingTags.Any())
                {
                    RecipeDef recipe = DefDatabase<RecipeDef>.GetNamed(("Make" + ammoDef.defName), false);
                    if (recipe == null)
                    {
                        Log.Error("CE ammo injector found no recipe named Make" + ammoDef.defName);
                    }
                    else
                    {
                        // Go through all crafting tags and add to the appropriate benches
                        foreach (string curTag in craftingTags)
                        {
                            ThingDef bench;
                            if (curTag == enableCraftingTag)
                            {
                                bench = CE_ThingDefOf.AmmoBench;
                            }
                            else
                            {
                                // Parse tag for bench def
                                if (curTag.Length <= enableCraftingTag.Length + 1)
                                {
                                    Log.Error("CE :: AmmoInjector trying to inject " + ammoDef.ToString() + " but " + curTag + " is not a valid crafting tag, valid formats are: " + enableCraftingTag + " and " + enableCraftingTag + "_defNameOfCraftingBench");
                                    continue;
                                }
                                var benchName = curTag.Remove(0, enableCraftingTag.Length + 1);
                                bench = DefDatabase<ThingDef>.GetNamed(benchName, false);
                                if (bench == null)
                                {
                                    Log.Error("CE :: AmmoInjector trying to inject " + ammoDef.ToString() + " but no crafting bench with defName=" + benchName + " could be found for tag " + curTag);
                                    continue;
                                }
                            }
                            ToggleRecipeOnBench(recipe, bench);
                            /*
                            // Toggle recipe
                            if (enabled)
                            {
                                recipe.recipeUsers.Add(bench);
                            }
                            else
                            {
                                recipe.recipeUsers.RemoveAll(x => x.defName == bench.defName);
                            }
                            */
                        }
                    }
                }
            }
            
            /*
        	if (canCraft)
        	{
            	// Set ammoCraftingStation.AllRecipes to null so it will reset
				_allRecipesCached.SetValue(AmmoCraftingStation, null);
				
				// Remove all bills which contain removed ammo types
				if (!enabled)
				{
                    if (Current.Game != null)
                    {
                        IEnumerable<Building> enumerable = Find.Maps.SelectMany(x => x.listerBuildings.AllBuildingsColonistOfDef(AmmoCraftingStation));
                        foreach (Building current in enumerable)
                        {
                            var billGiver = current as IBillGiver;
                            if (billGiver != null)
                            {
                                for (int i = 0; i < billGiver.BillStack.Count; i++)
                                {
                                    Bill bill = billGiver.BillStack[i];
                                    if (!AmmoCraftingStation.AllRecipes.Exists(r => bill.recipe == r))
                                    {
                                        billGiver.BillStack.Delete(bill);
                                    }
                                }
                            }
                        }
                    }
					
            		CE_Utility.allWeaponDefs.Clear();
				}
            }
            */
            
            return true;
        }

        private static void ToggleRecipeOnBench(RecipeDef recipeDef, ThingDef benchDef)
        {
            if (Controller.settings.EnableAmmoSystem)
            {
                if (recipeDef.recipeUsers == null)
                    recipeDef.recipeUsers = new List<ThingDef>();
                recipeDef.recipeUsers.Add(benchDef);
            }
            else
            {
                recipeDef.recipeUsers?.RemoveAll(x => x.defName == benchDef.defName);

                // Remove all bills for disabled recipes
                if (Current.Game != null)
                {
                    IEnumerable<Building> enumerable = Find.Maps.SelectMany(x => x.listerBuildings.AllBuildingsColonistOfDef(benchDef));
                    foreach (Building current in enumerable)
                    {
                        var billGiver = current as IBillGiver;
                        if (billGiver != null)
                        {
                            for (int i = 0; i < billGiver.BillStack.Count; i++)
                            {
                                Bill bill = billGiver.BillStack[i];
                                if (!benchDef.AllRecipes.Exists(r => bill.recipe == r))
                                {
                                    billGiver.BillStack.Delete(bill);
                                }
                            }
                        }
                    }
                }
            }
            _allRecipesCached.SetValue(benchDef, null);  // Set ammoCraftingStation.AllRecipes to null so it will reset
        }
    }
}
