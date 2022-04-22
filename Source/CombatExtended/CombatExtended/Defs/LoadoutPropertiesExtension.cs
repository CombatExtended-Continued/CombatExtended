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
        public AttachmentOption primaryAttachments;

        public FloatRange shieldMoney = FloatRange.Zero;
        public List<string> shieldTags;        
        public float shieldChance = 0;        
        public SidearmOption forcedSidearm;
        public List<SidearmOption> sidearms;
        public AmmoCategoryDef forcedAmmoCategory;

        private static List<ThingStuffPair> allWeaponPairs;
        private static List<ThingStuffPair> allShieldPairs;
        private static List<ThingStuffPair> workingWeapons = new List<ThingStuffPair>();
        private static List<ThingStuffPair> workingShields = new List<ThingStuffPair>();
        private static List<AttachmentLink> attachmentLinks = new List<AttachmentLink>();
        private static List<AttachmentLink> selectedAttachments = new List<AttachmentLink>();

        #endregion

        #region Methods

        // Copied from PawnWeaponGenerator.Reset() - 1:1 COPY!!! (Plus Shields)
        public static void Reset()
        {
            // Initialize weapons
            Predicate<ThingDef> isWeapon = (ThingDef td) => td.equipmentType == EquipmentType.Primary && !td.weaponTags.NullOrEmpty<string>();
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
               || (pawn.WorkTagIsDisabled(WorkTags.Violent))
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
                // Inventory load changed, need to update
                inventory.UpdateInventory();   
                // Try add attachments to the main weapon
                if(primaryAttachments != null && primary is WeaponPlatform platform)
                {
                    TryGenerateAttachments(inventory, platform, primaryAttachments);
                }
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

        public void TryGenerateAttachments(CompInventory inventory, WeaponPlatform weapon, AttachmentOption option)
        {            
            selectedAttachments.Clear();
            attachmentLinks.Clear();
            // First grab every possible attachment for this weapon
            attachmentLinks.AddRange(weapon.Platform.attachmentLinks);
            // If tags are specified then remove attachments that doesn't match them
            if (option.attachmentTags != null && option.attachmentTags.Count > 0)
            {
                // Remove attachments that are not in the options.
                attachmentLinks.RemoveAll(l => l.attachment.attachmentTags.All(s => !option.attachmentTags.Contains(s)));
            }            
            float availableWeight = inventory.GetAvailableWeight();
            float availableBulk = inventory.GetAvailableBulk();
            foreach (AttachmentLink link in attachmentLinks.InRandomOrder())
            {                
                if (selectedAttachments.Count >= option.attachmentCount.max || availableWeight <= 0 || availableBulk <= 0)
                {
                    break;
                }
                float weight = link.attachment.GetStatValueAbstract(StatDefOf.Mass);
                float bulk = link.attachment.GetStatValueAbstract(CE_StatDefOf.Bulk);                
                if (availableWeight < weight || availableBulk < bulk || selectedAttachments.Any(l => !l.CompatibleWith(link)))
                {
                    continue;
                }                               
                if (option.attachmentCount.min > selectedAttachments.Count || (Rand.ChanceSeeded(weapon.def.generateAllowChance, weapon.thingIDNumber ^ (int)weapon.def.shortHash ^ 28554824)))
                {
                    selectedAttachments.Add(link);
                    availableWeight -= weight;
                    availableBulk -= bulk;
                }                             
            }
            // Add the selected attachments to the weapons
            weapon.TargetConfig = selectedAttachments.Select(l => l.attachment).ToList();
            weapon.attachments.Clear();            
            weapon.attachments.AddRange(selectedAttachments);            
            // Recalcuate internal states.
            weapon.UpdateConfiguration();
        }

        private void TryGenerateWeaponWithAmmoFor(Pawn pawn, CompInventory inventory, SidearmOption option)
        {
            if (option.weaponTags.NullOrEmpty() || !Rand.Chance(option.generateChance))
            {
                return;
            }
            // Generate weapon - mostly based on PawnWeaponGenerator.TryGenerateWeaponFor()
            // START 1:1 COPY
            float randomInRange = pawn.kindDef.weaponMoney.RandomInRange;
            for (int i = 0; i < allWeaponPairs.Count; i++)
            {
                ThingStuffPair w = allWeaponPairs[i];
                if (w.Price <= randomInRange)
                {
                    if (option.weaponTags == null || option.weaponTags.Any((string tag) => w.thing.weaponTags.Contains(tag)))
                    {
                        if (w.thing.generateAllowChance >= 1f || Rand.ChanceSeeded(w.thing.generateAllowChance, pawn.thingIDNumber ^ (int)w.thing.shortHash ^ 28554824))
                        {
                            workingWeapons.Add(w);
                        }
                    }
                }
            }
            if (workingWeapons.Count == 0)
            {
                return;
            }
            // END 1:1 COPY
            // pawn.equipment.DestroyAllEquipment(DestroyMode.Vanish); --removed compared to sourcecode
            // Some 1:1 COPY below
            ThingStuffPair thingStuffPair;
            if (workingWeapons.TryRandomElementByWeight((ThingStuffPair w) => w.Commonality * w.Price, out thingStuffPair))
            {
                // Create the actual weapon and put it into inventory
                ThingWithComps thingWithComps = (ThingWithComps)ThingMaker.MakeThing(thingStuffPair.thing, thingStuffPair.stuff);                
                LoadWeaponWithRandAmmo(thingWithComps); //Custom
                int count; //Custom
                if (inventory.CanFitInInventory(thingWithComps, out count)) //Custom
                {
                    PawnGenerator.PostProcessGeneratedGear(thingWithComps, pawn);
                    if (inventory.container.TryAdd(thingWithComps)) //Custom
                    {
                        TryGenerateAmmoFor(thingWithComps, inventory, Mathf.RoundToInt(option.magazineCount.RandomInRange));
                        // Try to add the attachments
                        inventory.UpdateInventory();
                        if (option.attachments != null && thingWithComps is WeaponPlatform platform)
                        {
                            TryGenerateAttachments(inventory, platform, option.attachments);
                        }
                    }                    
                }
            }
            workingWeapons.Clear();
        }

        private void LoadWeaponWithRandAmmo(ThingWithComps gun)
        {
            CompAmmoUser compAmmo = gun.TryGetComp<CompAmmoUser>();
            if (compAmmo == null) return;
            if (!compAmmo.UseAmmo)
            {
                compAmmo.ResetAmmoCount();
                return;
            }
            // Determine ammo
            IEnumerable<AmmoDef> availableAmmo = compAmmo.Props.ammoSet.ammoTypes.Where(a => a.ammo.alwaysHaulable && !a.ammo.menuHidden && a.ammo.generateAllowChance > 0f).Select(a => a.ammo); //Running out of options. alwaysHaulable does exist in xml.

            AmmoDef ammoToLoad = availableAmmo.RandomElementByWeight(a => a.generateAllowChance);

            if (this.forcedAmmoCategory != null)
            {
                if (availableAmmo.Any(x => x.ammoClass == this.forcedAmmoCategory))
                {
                    ammoToLoad = availableAmmo.Where(x => x.ammoClass == this.forcedAmmoCategory).FirstOrFallback();
                }
            }

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
                unitCount = Mathf.Max(1, compAmmo.MagSize);  // Guns use full magazines as units
            }

            if (forcedAmmoCategory != null)
            {
                IEnumerable<AmmoDef> availableAmmo = compAmmo.Props.ammoSet.ammoTypes.Where(a => a.ammo.alwaysHaulable && !a.ammo.menuHidden && a.ammo.generateAllowChance > 0f).Select(a => a.ammo);
                if (availableAmmo.Any(x => x.ammoClass == forcedAmmoCategory))
                {
                    thingToAdd = availableAmmo.Where(x => x.ammoClass == forcedAmmoCategory).FirstOrFallback();
                }
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
            foreach (ThingStuffPair cur in allShieldPairs)
            {
                if (cur.Price < money
                    && shieldTags.Any(t => cur.thing.apparel.tags.Contains(t))
                    && (cur.thing.generateAllowChance >= 1f || Rand.ValueSeeded(pawn.thingIDNumber ^ 68715844) <= cur.thing.generateAllowChance)
                    && pawn.apparel.CanWearWithoutDroppingAnything(cur.thing)
                    && ApparelUtility.HasPartsToWear(pawn, cur.thing))
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
