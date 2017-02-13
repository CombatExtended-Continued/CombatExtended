using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using CombatExtended.Detours;

namespace CombatExtended
{
    [StaticConstructorOnStartup]
    internal static class AmmoInjector
    {
        private const string enableTradeTag = "CE_AutoEnableTrade";             // The trade tag which designates ammo defs for being automatically switched to Tradeability.Stockable
        private const string enableCraftingTag = "CE_AutoEnableCrafting";        // The trade tag which designates ammo defs for having their crafting recipes automatically added to the crafting table
        private static ThingDef ammoCraftingStationInt;                         // The crafting station to which ammo will be automatically added
        private static ThingDef ammoCraftingStation
        {
            get
            {
                if (ammoCraftingStationInt == null)
                    ammoCraftingStationInt = ThingDef.Named("AmmoBench");
                return ammoCraftingStationInt;
            }
        }

        static AmmoInjector()
        {
            LongEventHandler.QueueLongEvent(Inject, "LibraryStartup", false, null);
        }

        public static void Inject()
        {
            if (InjectAmmos()) Log.Message("Combat Realism :: Ammo injected");
            else Log.Error("Combat Realism :: Ammo injector failed to get injected");
        }

        public static bool InjectAmmos()
        {
            if (!ModSettings.enableAmmoSystem) return true;

            // Initialize list of all weapons so we don't have to iterate through all the defs, all the time
            CE_Utility.allWeaponDefs.Clear();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.IsWeapon && (def.canBeSpawningInventory || def.tradeability == Tradeability.Stockable || def.weaponTags.Contains("TurretGun")))
                    CE_Utility.allWeaponDefs.Add(def);
            }
            if (CE_Utility.allWeaponDefs.NullOrEmpty())
            {
                Log.Warning("CE Ammo Injector found no weapon defs");
                return true;
            }
            
            // Find all ammo using guns
            foreach (ThingDef weaponDef in CE_Utility.allWeaponDefs)
            {
                CompProperties_AmmoUser props = weaponDef.GetCompProperties<CompProperties_AmmoUser>();
                if (props != null && props.ammoSet != null && !props.ammoSet.ammoTypes.NullOrEmpty())
                {
                    foreach(ThingDef curDef in props.ammoSet.ammoTypes)
                    {
                        AmmoDef ammoDef = curDef as AmmoDef;
                        if(ammoDef != null)
                        {
                            // Enable trading
                            if (ammoDef.tradeTags.Contains(enableTradeTag))
                                ammoDef.tradeability = Tradeability.Stockable;

                            // Enable recipe
                            if (ammoDef.tradeTags.Contains(enableCraftingTag))
                            {
                                RecipeDef recipe = DefDatabase<RecipeDef>.GetNamed(("Make" + ammoDef.defName), false);
                                if (recipe == null)
                                {
                                    Log.Error("CE ammo injector found no recipe named Make" + ammoDef.defName);
                                }
                                else
                                {
                                    if (ammoCraftingStation == null)
                                    {
                                        Log.ErrorOnce("CE ammo injector crafting station is null", 84653201);
                                    }
                                    else
                                    {
                                        if (!recipe.recipeUsers.Contains(ammoCraftingStation))
                                        {
                                            recipe.recipeUsers.Add(ammoCraftingStation);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }
    }
}
