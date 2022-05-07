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

        public const string destroyWithAmmoDisabledTag = "CE_Ammo";               // The trade tag which automatically deleted this ammo with the ammo system disabled
        private const string enableTradeTag = "CE_AutoEnableTrade";             // The trade tag which designates ammo defs for being automatically switched to Tradeability.Stockable
        private const string enableCraftingTag = "CE_AutoEnableCrafting";        // The trade tag which designates ammo defs for having their crafting recipes automatically added to the crafting table
        // these ammo classes are disabled when simplified ammo is turned on
        private static HashSet<string> complexAmmoClasses = new HashSet<string>(new string[] {
            // pistol + rifle + high caliber
            "ArmorPiercing", "HollowPoint", "Sabot", "IncendiaryAP", "ExplosiveAP",
            // shotguns
            "Beanbag", "Slug",
            // advanced
            "ChargedAP"
        });

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
            ThingSetMakerUtility.Reset();   // Reset pool of spawnable ammos for quests, etc.
        }

        public static bool InjectAmmos()
        {
            bool enabled = Controller.settings.EnableAmmoSystem;
            bool simplifiedAmmo = Controller.settings.EnableSimplifiedAmmo;

            // Initialize list of all weapons
            CE_Utility.allWeaponDefs.Clear();

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.IsWeapon
                    && (def.generateAllowChance > 0
                        || def.tradeability.TraderCanSell()
                        || (def.weaponTags != null && def.weaponTags.Contains("TurretGun"))))
                    CE_Utility.allWeaponDefs.Add(def);
            }
            if (CE_Utility.allWeaponDefs.NullOrEmpty())
            {
                Log.Warning("CE Ammo Injector found no weapon defs");
                return true;
            }

            AddRemoveCaliberFromGunRecipes();

            var ammoDefs = new HashSet<ThingDef>();

            // Find all ammo using guns
            foreach (ThingDef weaponDef in CE_Utility.allWeaponDefs)
            {
                CompProperties_AmmoUser props = weaponDef.GetCompProperties<CompProperties_AmmoUser>();
                if (props != null && props.ammoSet != null && !props.ammoSet.ammoTypes.NullOrEmpty())
                {
                    // Union their ammoTypes -- since ammoDefs is a HashSet, duplicates are automatically removed
                    ammoDefs.UnionWith(props.ammoSet.ammoTypes.Select<AmmoLink, ThingDef>(x => x.ammo));
                }
                CompProperties_UnderBarrel propsGL = weaponDef.GetCompProperties<CompProperties_UnderBarrel>();
                if (propsGL != null && propsGL.propsUnderBarrel.ammoSet != null && !propsGL.propsUnderBarrel.ammoSet.ammoTypes.NullOrEmpty())
                {
                    ammoDefs.UnionWith(propsGL.propsUnderBarrel.ammoSet.ammoTypes.Select<AmmoLink, ThingDef>(x => x.ammo));
                }
            }
            /*
            bool canCraft = (AmmoCraftingStation != null);
            
            if (!canCraft)
            {
            	Log.ErrorOnce("CE ammo injector crafting station is null", 84653201);
            }
            */

            // Loop through all weaponDef's unique ammoType.ammo values
            foreach (AmmoDef ammoDef in ammoDefs)
            {
                //AFTER CE_Utility.allWeaponDefs is initiated, this sets each ammo to list its users & special effects in its DEF DESCRIPTION rather than its THING DESCRIPTION.
                //This is because the THING description ISN'T available during crafting - so people can now figure out what's different between ammo types.
                ammoDef.AddDescriptionParts();

                // mortar ammo will always be enabled, even if the ammo system is turned off
                bool ammoEnabled = enabled || ammoDef.isMortarAmmo;
                if (simplifiedAmmo && complexAmmoClasses.Contains(ammoDef.ammoClass.defName))
                    ammoEnabled = false;


                if (ammoDef.tradeTags != null)
                {
                    if (ammoDef.tradeTags.Contains(destroyWithAmmoDisabledTag))
                    {
                        // Toggle ammo visibility in the debug menu                        
                        ammoDef.menuHidden = !ammoEnabled;
                        ammoDef.destroyOnDrop = !ammoEnabled;
                    }

                    //Weapon defs aren't changed w.r.t crafting, trading, destruction on drop -- but the description is still added to the recipe
                    if (ammoDef.IsWeapon)
                        continue;

                    //  LX7: Commented this out for now as it's preventing mechanoid ammo from being sold.
                    //  If this is needed for something that can't be accomplished via XML, update it with mech ammo sellability in mind.
                    //if (!ammoDef.Users                                                                          //If there exists NO gun..
                    //    .Any(x => !x.destroyOnDrop                                                              //.. which DOESN'T destroy on drop (e.g all guns destroy on drop)
                    //                || (x.weaponTags != null && x.weaponTags.Contains("TurretGun")              //.. or IS part of a Turret..
                    //                    && DefDatabase<ThingDef>.AllDefs.Where(y => y.building?.turretGunDef == x)                //.. as long as ALL turrets using the gun are non-mechcluster turrets
                    //                        .All(y => !y.building?.buildingTags?.Contains(MechClusterGenerator.MechClusterMemberTag) ?? true))))
                    //    continue;                                                                               //Then this ammo's tradeability and craftability are ignored

                    // Toggle trading
                    var tradingTags = ammoDef.tradeTags.Where(t => t.StartsWith(enableTradeTag));
                    if (tradingTags.Any())
                    {
                        var curTag = tradingTags.First();

                        if (curTag == enableTradeTag)
                        {
                            ammoDef.tradeability = ammoEnabled ? Tradeability.All : Tradeability.None;
                        }
                        else
                        {
                            if (curTag.Length <= enableTradeTag.Length + 1)
                            {
                                Log.Error("Combat Extended :: AmmoInjector trying to inject " + ammoDef.ToString() + " but " + curTag + " is not a valid trading tag, valid formats are: " + enableTradeTag + " and " + enableTradeTag + "_levelOfTradeability");
                            }
                            else
                            {
                                var tradeabilityName = curTag.Remove(0, enableTradeTag.Length + 1);

                                ammoDef.tradeability = ammoEnabled
                                    ? (Tradeability)Enum.Parse(typeof(Tradeability), tradeabilityName, true)
                                    : Tradeability.None;
                            }
                        }
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
                                        Log.Error("Combat Extended :: AmmoInjector trying to inject " + ammoDef.ToString() + " but " + curTag + " is not a valid crafting tag, valid formats are: " + enableCraftingTag + " and " + enableCraftingTag + "_defNameOfCraftingBench");
                                        continue;
                                    }
                                    var benchName = curTag.Remove(0, enableCraftingTag.Length + 1);
                                    bench = DefDatabase<ThingDef>.GetNamed(benchName, false);
                                    if (bench == null)
                                    {
                                        Log.Error("Combat Extended :: AmmoInjector trying to inject " + ammoDef.ToString() + " but no crafting bench with defName=" + benchName + " could be found for tag " + curTag);
                                        continue;
                                    }
                                }
                                ToggleRecipeOnBench(recipe, bench, ammoEnabled);
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

        private static void ToggleRecipeOnBench(RecipeDef recipeDef, ThingDef benchDef, bool ammoEnabled)
        {
            if (ammoEnabled)
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
            benchDef.allRecipesCached = null;  // Set ammoCraftingStation.AllRecipes to null so it will reset
        }

        public static bool gunRecipesShowCaliber = false;
        public static void AddRemoveCaliberFromGunRecipes()
        {
            var shouldHaveLabels = (Controller.settings.EnableAmmoSystem && Controller.settings.ShowCaliberOnGuns);

            if (gunRecipesShowCaliber != shouldHaveLabels)
            {
                CE_Utility.allWeaponDefs.ForEach(x =>
                {
                    var ammoSet = x.GetCompProperties<CompProperties_AmmoUser>()?.ammoSet;

                    if (ammoSet != null)
                    {
                        RecipeDef recipeDef = DefDatabase<RecipeDef>.GetNamed("Make" + x.defName, false);

                        if (recipeDef != null)
                        {
                            var label = x.label + (shouldHaveLabels ? " (" + (string)ammoSet.LabelCap + ")" : "");

                            recipeDef.UpdateLabel("RecipeMake".Translate(label));           //Just setting recipeDef.label doesn't update Jobs nor existing recipeUsers. We need UpdateLabel.
                            recipeDef.jobString = "RecipeMakeJobString".Translate(label);   //The jobString should also be updated to reflect the name change.
                        }
                    }
                });

                gunRecipesShowCaliber = shouldHaveLabels;
            }
        }
    }
}
