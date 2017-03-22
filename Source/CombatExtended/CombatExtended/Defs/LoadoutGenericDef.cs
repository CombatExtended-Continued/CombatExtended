using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using RimWorld;
using Verse;

/* Was originally designed to take information from XML and on game startup compile the string to a linq but an apt method of doing that wasn't found.
 * Now each def is generated programatically and this *should* catch new guns and ammo from other mods.
 * Unfortunately an issue remains that if you create a situation where the sets of ThingDefs that two generics create, say set1 and set2.
 *  If set1 != set2 && set1 intersect set2 != null then the loadouts won't quite function right.
 * The first pass on generics had generics define an upper bound so that if you had a specific covered by a generic and the specific's count was > generic
 *  no more items would get picked up.  If the specific count < generic count then the generic would pick up generic desired count - specific have count.
 * The current system is to count up as when the other system failed a drop/pickup loop could start.  Now with an attempt to fill out each slot
 * instead not everything desired will be picked up but not loops should start.
 */
 
 /* Keep the following list up to date!  Try to avoid defining overlapping sets.  You will get a warning on startup if a very bad overlap occurs.
  * Currently defined Generics:
  * -for Meals - Pawns auto fetch a best food, generaly meals
  * -for Raw Foodstuff - Pawns can auto fetch raw food if no meals, also will fetch for animal training.
  * -for Drugs - Pawns can auto fetch drugs to fit a schedule.
  * -for Ammo of each Gun that uses CombatExtended Ammo.  Created programattically so should automatically get new guns/ammo.
  */
namespace CombatExtended
{
	/// <summary>
	/// LoadoutGenericDef handles Generic LoadoutSlots.
	/// </summary>
	[StaticConstructorOnStartup]
	public class LoadoutGenericDef : Verse.Def
	{
		#region Fields
		public LoadoutCountType defaultCountType = LoadoutCountType.dropExcess; // default: drop anything more than (default)Count.
		public int defaultCount = 1;
		private Predicate<ThingDef> _lambda;
		public bool isBasic = false;
		
		// The following group are intentionally left unsaved so that if the game state changes between saves the new value is calculated.
		private float _bulk;
		private float _mass;
		private bool _cachedVars = false;
		
		// used in automatically merging perfect conflicted ammo defs (most likely source of conflict)
		internal string itemString;
		internal static string dividingString = "CE_Generic_Divider".Translate();
		internal bool isAmmo;
		
		// (ProfoundDarkness) overlaps and conflicts are better handled later on so disabled but could still prove useful.
		// Having a key but a false value indicates that it's a perfect conflict.  True indicates imperfect conflict.  No key means no conflict.
		//public readonly Dictionary<LoadoutGenericDef, bool> conflicts = new Dictionary<LoadoutGenericDef, bool>();
		#endregion
		
		#region Constructors
		//UNDONE This doesn't define weapons as yet.  Want to get various things stable first RE inventory.
		//TODO: Need to define sane defaults for Food and Drug generics.  The defaults for Ammo come from the defs.
		static LoadoutGenericDef()
		{
			IEnumerable<ThingDef> everything = DefDatabase<ThingDef>.AllDefs;
			
			List<LoadoutGenericDef> defs = new List<LoadoutGenericDef>();
			
			LoadoutGenericDef generic = new LoadoutGenericDef();
			generic.defName = "GenericMeal";
			generic.description = "Generic Loadout for Meals.  Intended for compatibility with pawns automatically picking up a meal for themself.";
			generic.label = "CE_Generic_Meal".Translate();
			generic._lambda = td => td.IsNutritionGivingIngestible && td.ingestible.preferability >= FoodPreferability.MealAwful && !td.IsDrug;
			generic.isBasic = true;
			
			defs.Add(generic);
			//Log.Message(string.Concat("CombatExtended :: LoadoutGenericDef :: ", generic.LabelCap, " list: ", string.Join(", ", DefDatabase<ThingDef>.AllDefs.Where(t => generic.lambda(t)).Select(t => t.label).ToArray())));
			
			float targetNutrition = 0.85f;
			generic = new LoadoutGenericDef();
			generic.defName = "GenericRawFood";
			generic.description = "Generic Loadout for Raw Food.  Intended for compatibility with pawns automatically picking up raw food to train animals.";
			generic.label = "CE_Generic_RawFood".Translate();
			// Exclude drugs and corpses.  Also exclude any food worse than RawBad as in testing the pawns would not even pick it up for training.
			generic._lambda = td => td.IsNutritionGivingIngestible && td.ingestible.preferability <= FoodPreferability.RawTasty && td.ingestible.HumanEdible && td.plant == null && !td.IsDrug && !td.IsCorpse;
			//generic.defaultCount = Convert.ToInt32(Math.Floor(targetNutrition / everything.Where(td => generic.lambda(td)).Average(td => td.ingestible.nutrition)));
			generic.defaultCount = 1;
			generic.isBasic = true;
			
			defs.Add(generic);
			//Log.Message(string.Concat("CombatExtended :: LoadoutGenericDef :: ", generic.LabelCap, " list: ", string.Join(", ", DefDatabase<ThingDef>.AllDefs.Where(t => generic.lambda(t)).Select(t => t.label + " B(" + t.GetStatValueAbstract(CE_StatDefOf.Bulk) + ") M(" + t.GetStatValueAbstract(StatDefOf.Mass) + ")").ToArray())));

			generic = new LoadoutGenericDef();
			generic.defName = "GenericDrugs";
			generic.description = "Generic Loadout for Drugs.  Intended for compatibility with pawns automatically picking up drugs in compliance with drug policies.";
			generic.label = "CE_Generic_Drugs".Translate();
			// not really sure what defaultCount should be so leaving unset.
			generic._lambda = td => td.IsDrug;
			generic.isBasic = true;
			
			defs.Add(generic);
			//Log.Message(string.Concat("CombatExtended :: LoadoutGenericDef :: ", generic.LabelCap, " list: ", string.Join(", ", DefDatabase<ThingDef>.AllDefs.Where(t => generic.lambda(t)).Select(t => t.label).ToArray())));
			
			// now for the guns and ammo...
			
			// Get a list of guns that are player acquireable (not menuHidden but could also go with not dropOnDeath) which are ammo users with the CE verb.
			List<ThingDef> guns = DefDatabase<ThingDef>.AllDefs.Where(td => !td.menuHidden &&
			                                                          td.HasComp(typeof(CompAmmoUser)) && 
			                                                          td.Verbs.FirstOrDefault(v => v is VerbPropertiesCE) != null).ToList();
			string ammoLabel = "CE_Generic_Ammo".Translate();
			const string ammoDescription = "Generic Loadout ammo for {0}. Intended for generic collection of ammo for given gun.";
			foreach (ThingDef gun in guns)
			{
				// make sure the gun has ammo defined...
				if (gun.GetCompProperties<CompProperties_AmmoUser>().ammoSet.ammoTypes.Count <= 0)
					continue;
				generic = new LoadoutGenericDef();
				generic.defName = "GenericAmmo-" + gun.defName;
				generic.description = string.Format(ammoDescription, gun.LabelCap);
				generic.label = string.Format(ammoLabel, gun.LabelCap);
				generic.itemString = gun.LabelCap;
				generic.defaultCount = gun.GetCompProperties<CompProperties_AmmoUser>().magazineSize;
				//Consider all ammos that the gun can fire, take the average.  Could also use the min or max...  //TODO: Decide if max or average is apt.
				//generic.defaultCount = Convert.ToInt32(Math.Floor(DefDatabase<AmmoDef>.AllDefs
				//                                                  .Where(ad => gun.GetCompProperties<CompProperties_AmmoUser>().ammoSet.ammoTypes.Contains(ad))
				//                                                  .Average(ad => ad.defaultAmmoCount)));
				generic.defaultCountType = LoadoutCountType.pickupDrop; // we want ammo to get picked up.
				generic._lambda = td => td is AmmoDef && gun.GetCompProperties<CompProperties_AmmoUser>().ammoSet.ammoTypes.Contains(td);
				generic.isAmmo = true;
				defs.Add(generic);
				//Log.Message(string.Concat("CombatExtended :: LoadoutGenericDef :: ", generic.LabelCap, " list: ", string.Join(", ", DefDatabase<ThingDef>.AllDefs.Where(t => generic.lambda(t)).Select(t => t.label).ToArray())));
			}
			
			/* // (ProfoundDarkness) The methodology I'm using for LoadoutSlots allows for duplicates and partial overlaps (technically) so this code is less useful.
			// check for overlapping case and issue a warning if there is one... (yeah this is horribly inefficient but only happens once per game.)
			// First iteration looks for a merge-able perfect conflict and merges them when found.
			for (int i = 0; i <= defs.Count - 1; i++)
			{
				IEnumerable<ThingDef> a = everything.Where(t => defs[i].lambda(t));
				for (int j = i+1; j <= defs.Count - 1; j++)
				{
					IEnumerable<ThingDef> b = everything.Where(t => defs[j].lambda(t));
					IEnumerable<ThingDef> c = a.Intersect(b);
					if (c.Any())
					{
						if (a.Count() == c.Count() && b.Count() == c.Count() && defs[i].isAmmo && defs[j].isAmmo) // perfect conflict in ammo, can merge...
						{
							defs[i].itemString = string.Concat(defs[i].itemString, dividingString, defs[j].itemString);
							defs[i].label = string.Format(ammoLabel, defs[i].itemString);
							defs[i].description = string.Format(ammoDescription, defs[i].itemString);
							defs.RemoveAt(j);
							j--;
						}
					}
				}
			}
			
			// this iteration marks conflicts that couldn't be merged and issues warnings.
			for (int i = 0; i <= defs.Count - 1; i++)
			{
				IEnumerable<ThingDef> a = everything.Where(t => defs[i].lambda(t));
				for (int j = i+1; j <= defs.Count - 1; j++)
				{
					IEnumerable<ThingDef> b = everything.Where(t => defs[j].lambda(t));
					IEnumerable<ThingDef> c = a.Intersect(b);
					if (c.Any())
					{
						if (a.Count() != c.Count() && b.Count() != c.Count())
						{
							defs[i].conflicts.Add(defs[j], true);
							defs[j].conflicts.Add(defs[i], true);
							Log.Warning(string.Concat("CombatExtended :: LoadoutGenericDef :: ", defs[i].LabelCap, " and ", defs[j].LabelCap, 
								" share some members but are not identical and thus may cause problems with loadouts.  The following items are contained in both: ",
								string.Join(", ", c.Select(td => td.defName).ToArray())));
						} else {
							defs[i].conflicts.Add(defs[j], false);
							defs[j].conflicts.Add(defs[i], false);
							Log.Warning(string.Concat("CombatExtended :: LoadoutGenericDef :: ", defs[i].LabelCap, " and ", defs[j].LabelCap,
							                          " are a perfect conflict.  At the moment the code isn't aware of dealing with these but it can be if an issue is filed."));
						}
					}
				}
			}
			*/
			
			DefDatabase<LoadoutGenericDef>.Add(defs);
		}
		
		#endregion Constructors
		
		#region Properties
		
		/// <summary>
		/// Property gets/runs the lambda defining what ThingDefs are accepted by this def.
		/// </summary>
		public Predicate<ThingDef> lambda { get { return _lambda; } }
		
		/// <summary>
		/// Property gets the calculated bulk of this def.  This is determined at runtime based on stored Lambda rather than a static value.
		/// </summary>
		public float bulk
		{
			get
			{
				if (!_cachedVars)
					updateVars();
				return _bulk;
			}
		}
		
		/// <summary>
		/// Property gets the calculated mass of this def.  This is determined at runtime based on stored Lambda rather than a static value.
		/// </summary>
		public float mass
		{
			get
			{
				if (!_cachedVars)
					updateVars();
				return _mass;
			}
		}
		#endregion Properties
		
		#region Methods
		/// <summary>
		/// Handles updating this def's stored bulk/mass values.
		/// </summary>
		/// <remarks>Can be a bit expensive but only done once per def the first time such values are requested.</remarks>
		private void updateVars()
		{
			IEnumerable<ThingDef> matches;
			matches = DefDatabase<ThingDef>.AllDefs.Where(td => lambda(td));
        	_bulk = matches.Max(t => t.GetStatValueAbstract(CE_StatDefOf.Bulk));
        	_mass = matches.Max(t => t.GetStatValueAbstract(StatDefOf.Mass));
        	_cachedVars = true;
		}
		
		#endregion Methods
	}
}