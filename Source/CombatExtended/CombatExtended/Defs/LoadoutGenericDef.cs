using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using RimWorld;
using Verse;

/* Keep the following list up to date:
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
        private Predicate<ThingDef> _lambda = td => true;
        public ThingRequestGroup thingRequestGroup = ThingRequestGroup.HaulableEver;
        public bool isBasic = false;

        // The following group are intentionally left unsaved so that if the game state changes between saves the new value is calculated.
        private float _bulk;
        private float _mass;
        private bool _cachedVars = false;

        #endregion

        #region Constructors
        //UNDONE This doesn't define weapons as yet and the code might not handle that well.  Want to get various things stable first RE inventory.
        //       But we can define generics for short range, assault, pistol, melee.
        /*       (ProfoundDarkness) Some issues with weapons is that they have durability, quality, and often made of stuffs.
		 *                          Also could use a super-generic which fetches x clips for each weapon on the pawn (working on that for something else).
		 *                          I'm thinking we could add another button (more clutter) to each loadout slot which is only displayed if the item
		 *                           has key properties.  Clicking that button would show a new window which lets the user configure parameters like
		 *                           a range slider for durability, range slider for quality, and a checklist for stuffs (assuming is made of stuffs).
		 */

        /// <summary>
        /// This constructor gets run on startup of RimWorld and generates the various LoadoutGenericDef instance objects akin to having been loaded from xml.
        /// </summary>
        static LoadoutGenericDef()
        {
            // Used in a handful of places where all loaded ThingDefs are useful.
            IEnumerable<ThingDef> everything = DefDatabase<ThingDef>.AllDefs;

            // need to generate a list as that's how new defs are taken by DefDatabase.
            List<LoadoutGenericDef> defs = new List<LoadoutGenericDef>();


            LoadoutGenericDef generic = new LoadoutGenericDef();
            generic.defName = "GenericMeal";
            generic.description = "Generic Loadout for perishable meals.  Intended for compatibility with pawns automatically picking up a meal for themself.";
            generic.label = "CE_Generic_Meal".Translate();
            generic.defaultCountType = LoadoutCountType.pickupDrop; // Fits with disabling of RimWorld Pawn behavior of fetching meals themselves.
            generic._lambda = td => td.IsNutritionGivingIngestible && td.ingestible.preferability >= FoodPreferability.MealAwful && td.GetCompProperties<CompProperties_Rottable>()?.daysToRotStart <= 5 && !td.IsDrug;
            generic.isBasic = true;

            defs.Add(generic);
            //Log.Message(string.Concat("Combat Extended :: LoadoutGenericDef :: ", generic.LabelCap, " list: ", string.Join(", ", DefDatabase<ThingDef>.AllDefs.Where(t => generic.lambda(t)).Select(t => t.label).ToArray())));


            float targetNutrition = 0.85f;
            generic = new LoadoutGenericDef();
            generic.defName = "GenericRawFood";
            generic.description = "Generic Loadout for Raw Food.  Intended for compatibility with pawns automatically picking up raw food to train animals.";
            generic.label = "CE_Generic_RawFood".Translate();
            // Exclude drugs and corpses.  Also exclude any food worse than RawBad as in testing the pawns would not even pick it up for training.
            generic._lambda = td => td.IsNutritionGivingIngestible && td.ingestible.preferability <= FoodPreferability.RawTasty && td.ingestible.HumanEdible && td.plant == null && !td.IsDrug && !td.IsCorpse;
            generic.defaultCount = Convert.ToInt32(Math.Floor(targetNutrition / everything.Where(td => generic.lambda(td)).Average(td => td.ingestible.CachedNutrition)));
            //generic.defaultCount = 1;
            generic.isBasic = false; // doesn't need to be in loadouts by default as animal interaction talks to HoldTracker now.
                                     //TODO: Test pawns fetching raw food if no meal is available, if so then add a patch to have that talk to HoldTracker too.

            defs.Add(generic);
            //Log.Message(string.Concat("Combat Extended :: LoadoutGenericDef :: ", generic.LabelCap, " list: ", string.Join(", ", DefDatabase<ThingDef>.AllDefs.Where(t => generic.lambda(t)).Select(t => t.label + " B(" + t.GetStatValueAbstract(CE_StatDefOf.Bulk) + ") M(" + t.GetStatValueAbstract(StatDefOf.Mass) + ")").ToArray())));


            generic = new LoadoutGenericDef();
            generic.defName = "GenericDrugs";
            generic.defaultCount = 3;
            generic.description = "Generic Loadout for Drugs.  Intended for compatibility with pawns automatically picking up drugs in compliance with drug policies.";
            generic.label = "CE_Generic_Drugs".Translate();
            generic.thingRequestGroup = ThingRequestGroup.Drug;
            generic._lambda = td => td.IsDrug;
            generic.isBasic = true;

            defs.Add(generic);
            //Log.Message(string.Concat("Combat Extended :: LoadoutGenericDef :: ", generic.LabelCap, " list: ", string.Join(", ", DefDatabase<ThingDef>.AllDefs.Where(t => generic.lambda(t)).Select(t => t.label).ToArray())));


            generic = new LoadoutGenericDef();
            generic.defName = "GenericMedicine";
            generic.defaultCount = 5;
            generic.defaultCountType = LoadoutCountType.pickupDrop;
            generic.description = "Generic Loadout for Medicine.  Intended for pawns which will handle triage activities.";
            generic.label = "CE_Generic_Medicine".Translate();
            generic.thingRequestGroup = ThingRequestGroup.Medicine;
            generic._lambda = td => td.IsMedicine;
            generic.isBasic = true;
            defs.Add(generic);
            // now for the guns and ammo...

            // Get a list of guns that are player acquireable (not menuHidden but could also go with not dropOnDeath) which have expected comps/compProperties/verbs.
            List<ThingDef> guns = everything.Where(td => !td.IsMenuHidden()
                    && td.HasComp(typeof(CompAmmoUser))
                    && td.GetCompProperties<CompProperties_AmmoUser>() != null
                    && td.Verbs.FirstOrDefault(v => v is VerbPropertiesCE) != null).ToList();

            string ammoLabel = "CE_Generic_Ammo".Translate();
            const string ammoDescription = "Generic Loadout ammo for {0}. Intended for generic collection of ammo for given gun.";
            foreach (ThingDef gun in guns)
            {
                // make sure the gun has ammo defined...
                if (gun.GetCompProperties<CompProperties_AmmoUser>().ammoSet == null || gun.GetCompProperties<CompProperties_AmmoUser>().ammoSet.ammoTypes.Count <= 0)
                    continue;
                generic = new LoadoutGenericDef();
                generic.defName = "GenericAmmo-" + gun.defName;
                generic.description = string.Format(ammoDescription, gun.LabelCap);
                generic.label = string.Format(ammoLabel, gun.LabelCap);
                generic.defaultCount = gun.GetCompProperties<CompProperties_AmmoUser>().magazineSize;
                generic.defaultCountType = LoadoutCountType.pickupDrop; // we want ammo to get picked up.
                                                                        //generic._lambda = td => td is AmmoDef && gun.GetCompProperties<CompProperties_AmmoUser>().ammoSet.ammoTypes.Contains(td);
                generic.thingRequestGroup = ThingRequestGroup.HaulableEver;
                generic._lambda = td => td is AmmoDef && gun.GetCompProperties<CompProperties_AmmoUser>().ammoSet.ammoTypes.Any(al => al.ammo == td);
                defs.Add(generic);
                //Log.Message(string.Concat("Combat Extended :: LoadoutGenericDef :: ", generic.LabelCap, " list: ", string.Join(", ", DefDatabase<ThingDef>.AllDefs.Where(t => generic.lambda(t)).Select(t => t.label).ToArray())));
            }

            // finally we add all the defs generated to the DefDatabase.
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
            matches = DefDatabase<ThingDef>.AllDefs.Where(td => lambda(td) && thingRequestGroup.Includes(td));
            _bulk = matches.Max(t => t.GetStatValueAbstract(CE_StatDefOf.Bulk));
            _mass = matches.Max(t => t.GetStatValueAbstract(StatDefOf.Mass));
            _cachedVars = true;
        }

        #endregion Methods
    }
}
