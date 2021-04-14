using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    /// <summary>
    /// Just wraps an int so that the counts can be modified in the Dictionary without ending up modifying the Dictionary.
    /// </summary>
    public class Integer
    {
        public int value;
        public Integer(int num)
        {
            value = num;
        }
    }

    /// <summary>
	/// Primary responsibility of HoldTracker concept is to remember any items that the Pawn was instructed (forced) to pickup while having a loadout.
	/// </summary>
	/// <remarks>
	/// Secondarily is also the primary party to consult when looking to automatically drop items since it is aware of HoldTracker concept as well as Loadout(Specific/Generic) concepts.
	/// </remarks>
	static class Utility_HoldTracker
    {
        #region Fields
        static private int _tickLastPurge = 0;
        #endregion

        #region HoldTracker Methods

        /// <summary>
        /// Used when a pawn is about to be ordered to pickup a Thing.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="item"></param>
        /// <param name="count"></param>
        static public void Notify_HoldTrackerItem(this Pawn pawn, Thing item, int count)
        {
            // if the pawn doesn't have a normal loadout, nothing to do...
            if (pawn.GetLoadout().defaultLoadout)
                return;

            // find out if we are already remembering this thing on this pawn...
            List<HoldRecord> recs = LoadoutManager.GetHoldRecords(pawn);

            if (recs == null)
            {
                recs = new List<HoldRecord>();
                LoadoutManager.AddHoldRecords(pawn, recs);
            }

            // could check isHeld but that tells us if there is a record AND it's been picked up.
            HoldRecord rec = recs.FirstOrDefault(hr => hr.thingDef == item.def);
            if (rec != null)
            {
                if (rec.pickedUp)
                {
                    // modifying a record for which the pawn should have some of that thing in their inventory.
                    rec.count = GetMagazineAwareStackCount(pawn, rec.thingDef) + count;
                }
                else
                {
                    // modifying a record that hasn't been picked up... do it blind.
                    rec.count += count;
                }
                // useful debug message...
                //Log.Message(string.Concat("Job was issued to pickup items for this existing record: ", rec));
                return;
            }
            // if we got this far we know that there isn't a record being stored for this thingDef...
            rec = new HoldRecord(item.def, count);
            recs.Add(rec);
            // useful debug message...
            //Log.Message(string.Concat("Job was issued to pickup for this new record: ", rec));
        }

        /// <summary>
        /// Simply reports back if the pawn is tracking an item by ThingDef if it's been picked up.
        /// </summary>
        /// <param name="pawn">Pawn to check tracking on.</param>
        /// <param name="thing">Thing who's def should be checked if being held.</param>
        /// <returns></returns>
        public static bool HoldTrackerIsHeld(this Pawn pawn, Thing thing)
        {
            List<HoldRecord> recs = LoadoutManager.GetHoldRecords(pawn);
            if (recs != null && recs.Any(hr => hr.thingDef == thing.def))
                return true;
            return false;
        }

        /// <summary>
        /// Is there any hold tracker records for this pawn?  This doesn't care if an item is actually present in the pawn's inventory.
        /// </summary>
        /// <param name="pawn">Pawn who's HoldTracker is to be polled</param>
        /// <returns>bool indicating if the pawn has any HoldTracker records.</returns>
        public static bool HoldTrackerAnythingHeld(this Pawn pawn)
        {
            List<HoldRecord> recs = LoadoutManager.GetHoldRecords(pawn);
            if (recs == null || recs.NullOrEmpty())
                return false;
            return recs.Any(r => r.pickedUp);
        }

        public static void HoldTrackerClear(this Pawn pawn)
        {
            List<HoldRecord> recs = LoadoutManager.GetHoldRecords(pawn);
            recs.Clear();
        }

        /// <summary>
        /// This should be called periodically so that HoldTracker can remove items that are no longer in the inventory via a method which isn't being watched.
        /// </summary>
        /// <param name="pawn">The pawn who's tracker should be checked.</param>
        public static void HoldTrackerCleanUp(this Pawn pawn)
        {
            if (_tickLastPurge <= GenTicks.TicksAbs)
            {
                LoadoutManager.PurgeHoldTrackerRolls();
                _tickLastPurge = GenTicks.TicksAbs + GenDate.TicksPerDay;
            }
            List<HoldRecord> recs = LoadoutManager.GetHoldRecords(pawn);
            CompInventory inventory = pawn.TryGetComp<CompInventory>();
            if (recs == null || inventory == null)
                return;

            for (int i = recs.Count - 1; i > 0; i--)
            {
                if (recs[i].pickedUp && inventory.container.TotalStackCountOfDef(recs[i].thingDef) <= 0)
                    recs.RemoveAt(i);
            }
        }

        /// <summary>
        /// Called when a pawn is instructed to drop something as well as if the user explicitly specifies the item should no longer be held onto.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="thing">Thing who's def should be forgotten.</param>
        public static void HoldTrackerForget(this Pawn pawn, Thing thing)
        {
            List<HoldRecord> recs = LoadoutManager.GetHoldRecords(pawn);
            if (recs == null)
            {
                Log.Error(string.Concat(pawn.Name, " wasn't being tracked by HoldTracker and tried to forget a ThingDef ", thing.def, "."));
                return;
            }
            HoldRecord rec = recs.FirstOrDefault(hr => hr.thingDef == thing.def);
            if (rec != null)
                recs.RemoveAt(recs.IndexOf(rec));
        }

        /// <summary>
        /// Makes it convenient to fetch a pawn's holdTracker (List of HoldRecords).
        /// </summary>
        /// <param name="pawn">Pawn to fetch the records for.</param>
        /// <returns>List of HoldRecords otherwise known as the Pawn's holdTracker.</returns>
        public static List<HoldRecord> GetHoldRecords(this Pawn pawn)
        {
            return LoadoutManager.GetHoldRecords(pawn);
        }

        #endregion

        #region Loadout/Holdtracker methods.

        /// <summary>
        /// Does a check on the pawn's inventory to determine if there is something that should be dropped.  See GetExcessEquipment and GetExcessThing.
        /// </summary>
        /// <returns>bool, true if there is something the pawn needs to drop.</returns>
        static public bool HasExcessThing(this Pawn pawn)
        {
            Thing ignore1;
            int ignore2;
            ThingWithComps ignore3;
            return GetExcessEquipment(pawn, out ignore3) || GetExcessThing(pawn, out ignore1, out ignore2);
        }

        /// <summary>
        /// Similar to GetExcessThing though narrower in scope.  If there is NOT a loadout which covers the equipped item, it should be dropped. 
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="dropEquipment">Thing which should be unequiped.</param>
        /// <returns>bool, true if there is equipment that should be unequipped.</returns>
        static public bool GetExcessEquipment(this Pawn pawn, out ThingWithComps dropEquipment)
        {
            Loadout loadout = pawn.GetLoadout();
            dropEquipment = null;
            if (loadout == null || (loadout != null && loadout.Slots.NullOrEmpty()) || pawn.equipment?.Primary == null)
                return false;

            if (pawn.IsItemQuestLocked(pawn.equipment?.Primary))
            {
                return false;
            }

            //Check if equipment is part of the loadout
            LoadoutSlot eqSlot = loadout.Slots.FirstOrDefault(s => s.count >= 1 && ((s.thingDef != null && s.thingDef == pawn.equipment.Primary.def)
                                                                                    || (s.genericDef != null && s.genericDef.lambda(pawn.equipment.Primary.def))));

            //Check if equipment is in the forced pick-up items list
            HoldRecord eqRecord = pawn.GetHoldRecords()?.FirstOrDefault(s => s.count >= 1 && s.thingDef != null && s.thingDef == pawn.equipment.Primary.def);

            if (eqSlot == null && eqRecord == null)
            {
                dropEquipment = pawn.equipment.Primary;
                return true;
            }
            return false;
        }


        /// <summary>
        /// Shorthand to find out if there is more stuff to drop without caring about what there is to drop.
        /// </summary>
        /// <param name="pawn">Pawn who's inventory is to be checked against their Loadout and HoldRecord.</param>
        /// <returns>bool true indicates something to drop.</returns>
        public static bool HasAnythingForDrop(this Pawn pawn)
        {
            Thing ignore1;
            int ignore2;
            return GetAnythingForDrop(pawn, out ignore1, out ignore2);
        }

        /// <summary>
        /// Called when trying to find something to drop (ie coming back from a caravan).  This is useful even on pawns without a loadout. 
        /// </summary>
        /// <param name="dropThing">Thing to be dropped from inventory.</param>
        /// <param name="dropCount">Amount to drop from inventory.</param>
        /// <returns></returns>
        static public bool GetAnythingForDrop(this Pawn pawn, out Thing dropThing, out int dropCount)
        {
            dropThing = null;
            dropCount = 0;

            if (pawn.inventory == null || pawn.inventory.innerContainer == null)
                return false;

            Loadout loadout = pawn.GetLoadout();
            if (loadout == null || loadout.Slots.NullOrEmpty())
            {
                List<HoldRecord> recs = LoadoutManager.GetHoldRecords(pawn);
                if (recs != null)
                {
                    // hand out any inventory item not covered by a HoldRecord.
                    foreach (Thing thing in pawn.inventory.innerContainer)
                    {
                        if (pawn.IsItemQuestLocked(thing))
                        {
                            continue;   //quest requirements prevent this from being dropped, skip to next thing in inventory
                        }
                        int numContained = pawn.inventory.innerContainer.TotalStackCountOfDef(thing.def);
                        HoldRecord rec = recs.FirstOrDefault(hr => hr.thingDef == thing.def);
                        if (rec == null)
                        {
                            // we don't have a HoldRecord for this thing, drop it.
                            dropThing = thing;
                            dropCount = numContained > dropThing.stackCount ? dropThing.stackCount : numContained;
                            return true;
                        }

                        if (numContained > rec.count)
                        {
                            dropThing = thing;
                            dropCount = numContained - rec.count;
                            dropCount = dropCount > dropThing.stackCount ? dropThing.stackCount : dropCount;
                            return true;
                        }
                    }
                }
                else
                {
                    // we have nither a HoldTracker nor a Loadout that we can ask, so just pick a random non-quest item from Inventory.
                    dropThing = pawn.inventory.innerContainer.Where(inventoryItem => !pawn.IsItemQuestLocked(inventoryItem))?.RandomElement();
                    dropCount = dropThing?.stackCount ?? 0;
                }
            }
            else
            {
                // hand out an item from GetExcessThing...
                return GetExcessThing(pawn, out dropThing, out dropCount);
            }

            return false;
        }

        /// <summary>
        /// Gets the Pawn's inventory as well as equipment and returns a dictionary keyed by ThingDef.
        /// </summary>
        /// <param name="pawn">Pawn to get the storage information from.</param>
        /// <returns>Dictionary containing keys of ThingDef and values of InventoryCount.</returns>
        public static Dictionary<ThingDef, Integer> GetStorageByThingDef(this Pawn pawn)
        {
            Dictionary<ThingDef, Integer> storage = new Dictionary<ThingDef, Integer>();
            CompAmmoUser gun;
            // get the pawn's weapon.  Also want ammo in the weapon.
            if (pawn.equipment?.Primary != null)
            {
                storage.Add(pawn.equipment.Primary.def, new Integer(1));
                gun = pawn.equipment.Primary.TryGetComp<CompAmmoUser>();
                if (gun != null && gun.UseAmmo)
                    storage.Add(gun.CurrentAmmo, new Integer(gun.CurMagCount));
            }
            // get the pawn's inventory
            foreach (Thing thing in pawn.inventory.innerContainer)
            {
                Thing thing2 = thing.GetInnerIfMinified();
                if (!storage.ContainsKey(thing2.def))
                    storage.Add(thing2.def, new Integer(0));
                storage[thing2.def].value += thing2.stackCount;
                gun = thing2.TryGetComp<CompAmmoUser>();
                if (gun != null && gun.UseAmmo)
                {
                    if (storage.ContainsKey(gun.CurrentAmmo))
                        storage[gun.CurrentAmmo].value += gun.CurMagCount;
                    else
                        storage.Add(gun.CurrentAmmo, new Integer(gun.CurMagCount));
                }
            }

            return storage;
        }

        /// <summary>
        /// Find an item that should be dropped from the pawn's inventory and how much to drop.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="dropThing">The thing which should be dropped.</param>
        /// <param name="dropCount">The amount to drop.</param>
        /// <returns>bool, true indicates that the out variables are filled with something to do work on (drop).</returns>
        // NOTE (ProfoundDarkness): Ended up doing this by nibbling away at the pawn's inventory (or dictionary representation of ThingDefs/Count).
        //  Probably not efficient but was easier to handle atm.
        static public bool GetExcessThing(this Pawn pawn, out Thing dropThing, out int dropCount)
        {
            //(ProfoundDarkness) Thanks to erdelf on the RimWorldMod discord for helping me figure out some dictionary stuff and C# concepts related to 'Primitives' (pass by Value).
            CompInventory inventory = pawn.TryGetComp<CompInventory>();
            Loadout loadout = pawn.GetLoadout();
            List<HoldRecord> records = LoadoutManager.GetHoldRecords(pawn);
            dropThing = null;
            dropCount = 0;

            if (inventory == null || inventory.container == null || loadout == null || loadout.Slots.NullOrEmpty())
                return false;

            Dictionary<ThingDef, Integer> listing = GetStorageByThingDef(pawn);

            // iterate over specifics and generics and Chip away at the dictionary.
            foreach (LoadoutSlot slot in loadout.Slots)
            {
                if (slot.thingDef != null && listing.ContainsKey(slot.thingDef))
                {
                    listing[slot.thingDef].value -= slot.count;
                    if (listing[slot.thingDef].value <= 0)
                        listing.Remove(slot.thingDef);
                }
                if (slot.genericDef != null)
                {
                    List<ThingDef> killKeys = new List<ThingDef>();
                    int desiredCount = slot.count;
                    // find dictionary entries which corespond to covered slot.
                    foreach (ThingDef def in listing.Keys.Where(td => slot.genericDef.lambda(td)))
                    {
                        listing[def].value -= desiredCount;
                        if (listing[def].value <= 0)
                        {
                            desiredCount = 0 - listing[def].value;
                            killKeys.Add(def); // the thing in inventory is exausted, forget about it.
                        }
                        else
                        {
                            break; // we have satisifed this loadout so no need to keep enumerating.
                        }
                    }
                    // cleanup dictionary.
                    foreach (ThingDef def in killKeys)
                        listing.Remove(def);
                }
            }

            // if there is something left in the dictionary, that is what is to be dropped.
            // Complicated by the fact that we now consider ammo in guns as part of the inventory...
            if (listing.Any())
            {
                if (records != null && !records.NullOrEmpty())
                {
                    // look at each remaining 'uneaten' thingdef in pawn's inventory.
                    foreach (ThingDef def in listing.Keys)
                    {
                        HoldRecord rec = records.FirstOrDefault(r => r.thingDef == def);
                        if (rec == null)
                        {
                            // the item we have extra of has no HoldRecord, drop it.
                            dropThing = inventory.container.FirstOrDefault(t => t.def == def && !pawn.IsItemQuestLocked(t));
                            if (dropThing != null)
                            {
                                dropCount = listing[def].value > dropThing.stackCount ? dropThing.stackCount : listing[def].value;
                                return true;
                            }
                        }
                        else if (rec.count < listing[def].value)
                        {
                            // the item we have extra of HAS a HoldRecord but the amount carried is above the limit of the HoldRecord, drop extra.
                            dropThing = pawn.inventory.innerContainer.FirstOrDefault(t => t.def == def && !pawn.IsItemQuestLocked(t));
                            if (dropThing != null)
                            {
                                dropCount = listing[def].value - rec.count;
                                dropCount = dropCount > dropThing.stackCount ? dropThing.stackCount : dropCount;
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    foreach (ThingDef def in listing.Keys)
                    {
                        dropThing = inventory.container.FirstOrDefault(t => t.GetInnerIfMinified().def == def && !pawn.IsItemQuestLocked(t));
                        if (dropThing != null)
                        {
                            dropCount = listing[def].value > dropThing.stackCount ? dropThing.stackCount : listing[def].value;
                            return true;
                        }
                    }
                }
            } // else
            return false;
        }

        /// <summary>
        /// Gets the total count of a thingDef in a pawn's inventory.
        /// If thingDef is an ammo type, result will include rounds inside weapons magazines, matching behaviour in GetExcessThing.
        /// </summary>
        /// <param name="pawn">The pawn to check for stack count.</param>
        /// <param name="thingDef">The thingDef to look for.</param>
        /// <returns>Inventory count for specified item.</returns>
        static private int GetMagazineAwareStackCount(Pawn pawn, ThingDef thingDef)
        {
            int itemCount = 0;

            //Add stack count from inventory
            CompInventory inventory = pawn?.TryGetComp<CompInventory>();
            itemCount += inventory?.container?.TotalStackCountOfDef(thingDef) ?? 0;
            
            if (thingDef is AmmoDef)
            {
                //Add stack count from equipped primary (magazine ammo)
                if (pawn?.equipment?.Primary != null)
                {
                    CompAmmoUser ammoUser = pawn.equipment.Primary.TryGetComp<CompAmmoUser>();
                    if (ammoUser != null && ammoUser.HasMagazine && ammoUser.CurrentAmmo == thingDef)
                    {
                        itemCount += ammoUser.CurMagCount;
                    }
                }

                //Add stack count from inventory stored weapons (magazine ammo)
                if (inventory?.container != null)
                {
                    foreach (Thing inventoryItem in inventory.container)
                    {
                        CompAmmoUser ammoUser = inventoryItem.TryGetComp<CompAmmoUser>();
                        if (ammoUser != null && ammoUser.HasMagazine && ammoUser.CurrentAmmo == thingDef)
                        {
                            itemCount += ammoUser.CurMagCount;
                        }
                    }
                }
            }
            return itemCount;
        }
        #endregion
    }
}