using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class LoadoutPropertiesExtension : DefModExtension
    {
        #region Fields

        public FloatRange primaryMagazineCount = FloatRange.Zero;
        public FloatRange shieldMoney = FloatRange.Zero;
        public List<string> shieldTags;
        public float shieldChance = 0;
        public SidearmOption forcedSidearm;
        public List<SidearmOption> sidearms;

        private static List<ThingStuffPair> allWeaponPairs;
        private static List<ThingStuffPair> allShieldPairs;
        private static List<ThingStuffPair> workingWeapons = new List<ThingStuffPair>();
        private static List<ThingStuffPair> workingShields = new List<ThingStuffPair>();

        #endregion

        #region Methods

        // Copied from PawnWeaponGenerator.Reset()
        public static void Reset()
        {
            // Initialize weapons
            Predicate<ThingDef> isWeapon = (ThingDef td) => td.equipmentType == EquipmentType.Primary && td.canBeSpawningInventory && !td.weaponTags.NullOrEmpty<string>();
            allWeaponPairs = ThingStuffPair.AllWith(isWeapon);
            foreach (ThingDef thingDef in from td in DefDatabase<ThingDef>.AllDefs
                                          where isWeapon(td)
                                          select td)
            {
                float num = allWeaponPairs.Where((ThingStuffPair pa) => pa.thing == thingDef).Sum((ThingStuffPair pa) => pa.Commonality);
                float num2 = thingDef.generateCommonality / num;
                if (num2 != 1f)
                {
                    for (int i = 0; i < allWeaponPairs.Count; i++)
                    {
                        ThingStuffPair thingStuffPair = allWeaponPairs[i];
                        if (thingStuffPair.thing == thingDef)
                        {
                            allWeaponPairs[i] = new ThingStuffPair(thingStuffPair.thing, thingStuffPair.stuff, thingStuffPair.commonalityMultiplier * num2);
                        }
                    }
                }
            }
            // Initialize shields
            allShieldPairs = ThingStuffPair.AllWith(td => td.thingClass == typeof(Apparel_Shield));
        }

        public void GenerateLoadoutFor(Pawn pawn)
        {
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) 
                || (pawn.story?.WorkTagIsDisabled(WorkTags.Violent) ?? false) 
                || !pawn.RaceProps.ToolUser)
                return;

            var inventory = pawn.TryGetComp<CompInventory>();
            if (inventory == null)
            {
                Log.Error("CE tried generating loadout for " + pawn.ToStringSafe() + " without CompInventory");
                return;
            }

            // Generate forced sidearm
            if (forcedSidearm != null)
            {
                TryGenerateWeaponWithAmmoFor(pawn, inventory, forcedSidearm);
            }

            // Generate primary ammo
            var primary = pawn.equipment.Primary;
            if (primary != null)
            {
                LoadWeaponWithRandAmmo(primary);
                inventory.UpdateInventory();    // Inventory load changed, need to update
                TryGenerateAmmoFor(pawn.equipment.Primary, inventory, Mathf.RoundToInt(primaryMagazineCount.RandomInRange));
            }

            // Generate shield
            TryGenerateShieldFor(pawn, inventory, primary);

            // Generate sidearms and ammo
            if (!sidearms.NullOrEmpty())
            {
                foreach (SidearmOption current in sidearms)
                {
                    TryGenerateWeaponWithAmmoFor(pawn, inventory, current);
                }
            }
        }

        private void TryGenerateWeaponWithAmmoFor(Pawn pawn, CompInventory inventory, SidearmOption option)
        {
            if (option.weaponTags.NullOrEmpty() || !Rand.Chance(option.generateChance))
            {
                return;
            }
            // Generate weapon - mostly based on PawnWeaponGenerator.TryGenerateWeaponFor()
            float money = option.sidearmMoney.RandomInRange;
            foreach (ThingStuffPair cur in allWeaponPairs)
            {
                if (cur.Price <= money 
                    && option.weaponTags.Any(t => cur.thing.weaponTags.Contains(t))
                    && (cur.thing.generateAllowChance >= 1f || Rand.ValueSeeded(pawn.thingIDNumber ^ 28554635) <= cur.thing.generateAllowChance))
                {
                    workingWeapons.Add(cur);
                }
            }
            if (workingWeapons.Count == 0) return;
            ThingStuffPair pair;
            if(workingWeapons.TryRandomElementByWeight(p => p.Commonality * p.Price, out pair))
            {
                // Create the actual weapon and put it into inventory
                var eq = (ThingWithComps)ThingMaker.MakeThing(pair.thing, pair.stuff);
                LoadWeaponWithRandAmmo(eq);
                int count;
                if (inventory.CanFitInInventory(eq, out count))
                {
                    PawnGenerator.PostProcessGeneratedGear(eq, pawn);
                    if (inventory.container.TryAdd(eq))
                    {
                        TryGenerateAmmoFor(eq, inventory, Mathf.RoundToInt(option.magazineCount.RandomInRange));
                    }
                }
            }
            workingWeapons.Clear();
        }

        private void LoadWeaponWithRandAmmo(ThingWithComps gun)
        {
            var compAmmo = gun.TryGetComp<CompAmmoUser>();
            if (compAmmo == null) return;
            if (!compAmmo.UseAmmo)
            {
                compAmmo.ResetAmmoCount();
                return;
            }
            // Determine ammo
            var availableAmmo = compAmmo.Props.ammoSet.ammoTypes.Where(a => a.ammo.canBeSpawningInventory).Select(a => a.ammo);
            var ammoToLoad = availableAmmo.RandomElementByWeight(a => a.generateCommonality);
            compAmmo.ResetAmmoCount(ammoToLoad);
        }

        private void TryGenerateAmmoFor(ThingWithComps gun, CompInventory inventory, int ammoCount)
        {
            if (ammoCount <= 0) return;
            ThingDef thingToAdd;
            int unitCount = 1;  // How many ammo things to add per ammoCount
            var compAmmo = gun.TryGetComp<CompAmmoUser>();
            if (compAmmo == null || !compAmmo.UseAmmo)
            {
                if (gun.TryGetComp<CompEquippable>().PrimaryVerb.verbProps.verbClass == typeof(Verb_ShootCEOneUse))
                {
                    thingToAdd = gun.def;   // For one-time use weapons such as grenades, add duplicates instead of ammo
                }
                else
                {
                    return;
                }
            }
            else
            {
                // Generate currently loaded ammo
                thingToAdd = compAmmo.CurrentAmmo;
                unitCount = Mathf.Max(1, compAmmo.Props.magazineSize);  // Guns use full magazines as units
            }
            var ammoThing = thingToAdd.MadeFromStuff ? ThingMaker.MakeThing(thingToAdd, gun.Stuff) : ThingMaker.MakeThing(thingToAdd);
            ammoThing.stackCount = ammoCount * unitCount;
            int maxCount;
            if (inventory.CanFitInInventory(ammoThing, out maxCount))
            {
                if (maxCount < ammoThing.stackCount) ammoThing.stackCount = maxCount - (maxCount % unitCount);
                inventory.container.TryAdd(ammoThing);
            }
        }

        private void TryGenerateShieldFor(Pawn pawn, CompInventory inventory, ThingWithComps primary)
        {
            if ((primary != null && !primary.def.weaponTags.Contains(Apparel_Shield.OneHandedTag))
                || shieldTags.NullOrEmpty()
                || pawn.apparel == null
                || !Rand.Chance(shieldChance))
                return;

            var money = shieldMoney.RandomInRange;
            foreach(ThingStuffPair cur in allShieldPairs)
            {
                if(cur.Price < money
                    && shieldTags.Any(t => cur.thing.apparel.tags.Contains(t))
                    && (cur.thing.generateAllowChance >= 1f || Rand.ValueSeeded(pawn.thingIDNumber ^ 68715844) <= cur.thing.generateAllowChance)
                    && pawn.apparel.CanWearWithoutDroppingAnything(cur.thing))
                {
                    workingShields.Add(cur);
                }
            }
            if (workingShields.Count == 0) return;
            ThingStuffPair pair;
            if (workingShields.TryRandomElementByWeight(p => p.Commonality * p.Price, out pair))
            {
                var shield = (Apparel)ThingMaker.MakeThing(pair.thing, pair.stuff);
                int count;
                if (inventory.CanFitInInventory(shield, out count, false, true))
                {
                    pawn.apparel.Wear(shield);
                }
            }
            workingShields.Clear();
        }

        #endregion
    }
}
