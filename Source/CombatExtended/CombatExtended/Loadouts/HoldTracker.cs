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
    public class ThingDefCounter
    {
    	public int count;
    	public ThingDefCounter(int num)
    	{
    		count = num;
    	}
    }
        
	/// <summary>
	/// Primary responsibility of HoldTracker concept is to remember any items that the Pawn was instructed (forced) to pickup while having a loadout.
	/// </summary>
	/// <remarks>
	/// Secondarily is also the primary party to consult when looking to automatically drop items since it is aware of HoldTracker concept as well as Loadout(Specific/Generic) concepts.
	/// </remarks>
	static class HoldTracker
	{
		internal static readonly Dictionary<Pawn, Dictionary<ThingDef, HoldRecord>> tracker = new Dictionary<Pawn, Dictionary<ThingDef, HoldRecord>>(); // track what the pawn is holding to not drop.
		
		/// <summary>
		/// Used when a pawn is about to be ordered to pickup a Thing.
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="job"></param>
		static public void Notify_HoldTrackerJob(this Pawn pawn, Job job)
		{
			// make sure it's the right kind of job.
			if (job.def != JobDefOf.TakeInventory)
				throw new ArgumentException();
			
			// if the pawn doesn't have a normal loadout, nothing to do...
			if (pawn.GetLoadout().defaultLoadout)
				return;
			
			// find out if we are already remembering this thing on this pawn...
			Dictionary<ThingDef,HoldRecord> recs;
			if (tracker.TryGetValue(pawn, out recs))
			{
				HoldRecord testRec;
				if (recs.TryGetValue(job.targetA.Thing.def, out testRec))
				{
					testRec.count += job.count;
					return;
				}
			} else {
				// we don't have any data for this pawn so generate the skeleton.
				recs = new Dictionary<ThingDef, HoldRecord>();
				tracker.Add(pawn, recs);
			}
			
			// if we got this far we know that there isn't a record being stored for this thingDef...
			HoldRecord rec = new HoldRecord(job.targetA.Thing.def, job.count);
			recs.Add(job.targetA.Thing.def, rec);
			Log.Message(string.Concat("Job was issued to pickup this record: ", rec));
		}
		
		/// <summary>
		/// Simply reports back if the pawn is tracking a held item.
		/// </summary>
		/// <param name="pawn">Pawn to check tracking on.</param>
		/// <param name="thing">Thing who's def should be checked if being held.</param>
		/// <returns></returns>
		public static bool HoldTrackerIsHeld(this Pawn pawn, Thing thing)
		{
			Dictionary<ThingDef, HoldRecord> recs;
			if (tracker.TryGetValue(pawn, out recs))
			{
				if (recs.ContainsKey(thing.def))
				    return true;
			}
			return false;
		}
		
		/// <summary>
		/// This should be called periodically so that HoldTracker can remove items that are no longer in the inventory via a method which isn't being watched.
		/// </summary>
		/// <param name="pawn">The pawn who's tracker should be checked.</param>
		public static void HoldTrackerCleanUp(this Pawn pawn)
		{
			Dictionary<ThingDef, HoldRecord> recs;
			CompInventory inventory = pawn.TryGetComp<CompInventory>();
			if (!tracker.TryGetValue(pawn, out recs) || inventory == null)
				return;
			
			List<ThingDef> killList = new List<ThingDef>(recs.Count); // avoid delays due to expanding memory.  Could take a tiny bit longer to instantiate the list but avoids slowdowns due to growth/copying.
			foreach (HoldRecord rec in recs.Values)
			{
				if (rec.pickedUp && inventory.container.TotalStackCountOfDef(rec.thingDef) < 0)
					killList.Add(rec.thingDef);
			}
			// do the removal
			foreach (ThingDef def in killList)
			{
				recs.Remove(def);
			}
		}
		
		/// <summary>
		/// Called when a pawn is instructed to drop something as well as if the user explicitly specifies the item should no longer be held onto.
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="thing">Thing who's def should be forgotten.</param>
		public static void HoldTrackerForget(this Pawn pawn, Thing thing)
		{
			Dictionary<ThingDef, HoldRecord> recs;
			if (tracker.TryGetValue(pawn, out recs))
			{
				HoldRecord rec;
				if (recs.TryGetValue(thing.def, out rec))
					recs.Remove(rec.thingDef);
			}
		}
		
		// Methods in this region are related to dropping and need to be both HoldTracker and Loadout aware.  Above here are methods more strictly HoldTracker related.
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
        /// <param name="dropEquipment">Thing which should be unequiped.</param>
        /// <returns>bool, true if there is equipment that should be unequipped.</returns>
        static public bool GetExcessEquipment(this Pawn pawn, out ThingWithComps dropEquipment)
        {
        	Loadout loadout = pawn.GetLoadout();
        	dropEquipment = null;
        	if (loadout == null || (loadout != null && loadout.Slots.NullOrEmpty()) || pawn.equipment?.Primary == null)
        		return false;
        	
        	LoadoutSlot eqSlot = loadout.Slots.FirstOrDefault(s => s.count >= 1 && ((s.thingDef != null && s.thingDef == pawn.equipment.Primary.def) 
    		                                                                        || (s.genericDef != null && s.genericDef.lambda(pawn.equipment.Primary.def))));
    		if (eqSlot == null)
    		{
    			dropEquipment = pawn.equipment.Primary;
    			return true;
    		}
    		return false;
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
        		Dictionary<ThingDef, HoldRecord> recs;
        		if (HoldTracker.tracker.TryGetValue(pawn, out recs))
        		{
	        		// hand out any inventory item not covered by a HoldRecord.
	        		foreach (Thing thing in pawn.inventory.innerContainer)
	        		{
	        			int numContained = pawn.inventory.innerContainer.TotalStackCountOfDef(thing.def);
	        			HoldRecord rec;
	        			if (!recs.TryGetValue(thing.def, out rec))
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
        		} else {
        			// we have nither a HoldTracker nor a Loadout that we can ask, so just pick stuff at random from Inventory.
        			dropThing = pawn.inventory.innerContainer.RandomElement<Thing>();
        			dropCount = dropThing.stackCount;
        		}
        	} else {
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
        public static Dictionary<ThingDef, ThingDefCounter> GetStorageByThingDef(this Pawn pawn)
        {
        	Dictionary<ThingDef, ThingDefCounter> storage = new Dictionary<ThingDef, ThingDefCounter>();
        	CompAmmoUser gun;
			// get the pawn's weapon.  Also want ammo in the weapon.
        	if (pawn.equipment?.Primary != null)
        	{
				storage.Add(pawn.equipment.Primary.def, new ThingDefCounter(1));
				gun = pawn.equipment.Primary.TryGetComp<CompAmmoUser>();
				if (gun != null)
					storage.Add(gun.currentAmmo, new ThingDefCounter(gun.curMagCount));
        	}
			// get the pawn's inventory
        	foreach (Thing thing in pawn.inventory.innerContainer)
			{
				if (!storage.ContainsKey(thing.def))
					storage.Add(thing.def, new ThingDefCounter(0));
				storage[thing.def].count += thing.stackCount;
				gun = thing.TryGetComp<CompAmmoUser>();
				if (gun != null)
					storage.Add(gun.currentAmmo, new ThingDefCounter(gun.curMagCount));
			}
        	
			return storage;
        }
        
        /// <summary>
        /// Find an item that should be dropped from the pawn's inventory and how much to drop.
        /// </summary>
        /// <param name="dropThing">The thing which should be dropped.</param>
        /// <param name="dropCount">The amount to drop.</param>
        /// <returns>bool, true indicates that the out variables are filled with something to do work on (drop).</returns>
        // NOTE (ProfoundDarkness): Ended up doing this by nibbling away at the pawn's inventory (or dictionary representation of ThingDefs/Count).
        //  Probably not efficient but was easier to handle atm.
        static public bool GetExcessThing(this Pawn pawn, out Thing dropThing, out int dropCount)
        {
	        //ProfoundDarkness: Thanks to erdelf on the RimWorldMod discord for helping me figure out some dictionary stuff and C# concepts related to 'Primitives' (pass by Value).
        	CompInventory inventory = pawn.TryGetComp<CompInventory>();
        	Loadout loadout = pawn.GetLoadout();
        	Dictionary<ThingDef, HoldRecord> records;
        	dropThing = null;
        	dropCount = 0;
        	
        	if (inventory == null || inventory.container == null || loadout == null || loadout.Slots.NullOrEmpty())
        		return false;
        	
        	if (!HoldTracker.tracker.TryGetValue(pawn, out records))
        		records = new Dictionary<ThingDef, HoldRecord>();
        	
        	Dictionary<ThingDef, ThingDefCounter> listing = GetStorageByThingDef(pawn);
        	
        	// iterate over specifics and generics and Chip away at the dictionary.
        	foreach (LoadoutSlot slot in loadout.Slots)
        	{
        		if (slot.thingDef != null && listing.ContainsKey(slot.thingDef))
        		{
					listing[slot.thingDef].count -= slot.count;
					if (listing[slot.thingDef].count <= 0)
						listing.Remove(slot.thingDef);
        		}
        		if (slot.genericDef != null)
        		{
        			List<ThingDef> killKeys = new List<ThingDef>();
        			int desiredCount = slot.count;
					// find dictionary entries which corespond to covered slot.
					foreach (ThingDef def in listing.Keys.Where(td => slot.genericDef.lambda(td)))
        			{
						if (listing[def].count > 0)
						{
	        				listing[def].count -= desiredCount;
	        				if (listing[def].count <= 0)
	        				{
	        					desiredCount = 0 - listing[def].count;
	        					killKeys.Add(def); // the thing in inventory is exausted, forget about it.
	        				} else {
	        					break; // we have satisifed this loadout so no need to keep enumerating.
	        				}
						}
        			}
		        	// cleanup dictionary.
		        	foreach (ThingDef def in killKeys)
		        		listing.Remove(def);
        		}
        	}
        	
        	// if there is something left in the dictionary, that is what is to be dropped.
        	if (listing.Any())
        	{
        		if (tracker.TryGetValue(pawn, out records))
        		{
        			// look at each remaining 'uneaten' thingdef in pawn's inventory.
        			foreach (ThingDef def in listing.Keys)
        			{
        				// if we have a record (HoldTracker) for that thingdef...
        				if (records.ContainsKey(def))
        				{
        					// and it's count is over what we are holding onto, drop some of it.
        					if (listing[def].count > records[def].count)
        					{
	        					dropThing = pawn.inventory.innerContainer.First(t => t.def == def);
	        					dropCount = listing[def].count - records[def].count;
	        					dropCount = dropCount > dropThing.stackCount ? dropThing.stackCount : dropCount;
	        					return true;
        					}
        				} else {
        					// didn't have a HoldTracker record for the thingdef so it's OK to drop.
        					dropThing = inventory.container.First(t => t.def == def);
        					dropCount = listing[def].count > dropThing.stackCount ? dropThing.stackCount : listing[def].count;
        					return true;
        				}
        			}
        		} else {
	        		// normally this would be bad but at this point we know that there is a thing with a matching def in the inventory and there is something in the dictionary.
	        		// we want the error since that indicates a violation of assumptions at this point.
	        		dropThing = inventory.container.First(t => t.def == listing.Keys.First());
	        		dropCount = listing[listing.Keys.First()].count > dropThing.stackCount ? dropThing.stackCount : listing[listing.Keys.First()].count;
	        		return true;
        		}
        	} // else
       		return false;
        }
        #endregion
 	}
}