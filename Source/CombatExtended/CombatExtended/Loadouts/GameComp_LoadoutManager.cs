using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

/* Notes (ProfoundDarkness)
 * The GameComponent works a little different from MapComponent.
 * While a Game is being started it's possible for other code to try and use LoadoutManager, until FinalizeInit is called on us we shouldn't be responding to
 * requests.  This is because some information can leak from current game to loaded game if we don't block access to the object appropriately.
 * 
 * I ended up maintaining the interface between how things used to work and how things would work with a proper instance system.  There may have been a proper
 * way to retrieve the current instance of the current game QUICKLY but I didn't see it so instead all the static methods have a chance of returning nothing
 * without having done any work.  (They hide what's going on.)
 */

namespace CombatExtended
{
    public class LoadoutManager : GameComponent
    {
        #region Fields
        private Dictionary<Pawn, Loadout> _assignedLoadouts = new Dictionary<Pawn, Loadout>();
        private List<Loadout> _loadouts = new List<Loadout>();
        private List<Tracker> _trackers = new List<Tracker>();
        private Dictionary<Pawn, Tracker> _assignedTrackers = new Dictionary<Pawn, Tracker>();  // track what the pawn is holding to not drop.
        private static LoadoutManager _current; // there is a window where accessing the manager is invalid...
        #endregion Fields

        #region Constructors
        // constructor called on new/first game.
        public LoadoutManager(Game game)
        {
            // create a default empty loadout
            // there needs to be at least one default tagged loadout at all times
            _loadouts.Add(MakeDefaultLoadout());
            _current = null;    // this ensures the window of valid access is maintained by wiping the old instance.
        }
        // constructor called on Load Game.  When this gets called there can actually be 2 instances of our Component...
        public LoadoutManager()
        {
            // this constructor is also used by the scribe.
            _current = null;    // this ensures the window of valid access is maintained by wiping the old instance.
        }
        #endregion Constructors

        #region Properties
        public static Dictionary<Pawn, Loadout> AssignedLoadouts => _current != null ? _current._assignedLoadouts : new Dictionary<Pawn,Loadout>();

        public static Loadout DefaultLoadout { get { return _current != null ? _current._loadouts.First(l => l.defaultLoadout) : MakeDefaultLoadout(); } }

        public static List<Loadout> Loadouts => _current != null ? _current._loadouts : null;

        #endregion Properties

        #region Override Methods
        /// <summary>
        /// Called by RimWorld when the game is basically ready to be played.
        /// </summary>
        /// <remarks>If you allow access to your object before this is called (via _current or some other static instance variable) you WILL leak data between game states (bad).</remarks>
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            _current = Current.Game.GetComponent<LoadoutManager>(); // after this it's valid to be accessing the Component.
        }
        /* Unused CameComponent override methods:
         * GameComponentOnGUI() - is called basically any time something happens with the GUI.
         * GameComponentUpdate() - Uncertain, seems to get called any time the game state changes (including while paused if the player does something).
         * GameComponentTick() - Called on every tick of the game.
         * LoadedGame() - Called after the game is loaded at about the same time as StartedNewGame would get called.
         * StartedNewGame() - Called after a new game has been created (ie from the main menu -> start new game).
         */
        /// <summary>
        /// Load/Save handler.
        /// </summary>
        public override void ExposeData() // - called when saving a game as well as in the construction phase of creating a new instance on game load.
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                PurgeHoldTrackerRolls();
                PurgeLoadoutRolls();
            }

            Scribe_Collections.Look(ref _loadouts, "loadouts", LookMode.Deep);
            Scribe_Collections.Look<Pawn, Loadout>(ref _assignedLoadouts, "assignmentLoadouts", LookMode.Reference, LookMode.Reference);
            bool hasTrackers = _trackers.Any();
            Scribe_Values.Look<bool>(ref hasTrackers, "HasTrackers");
            if (hasTrackers)
            {
                Scribe_Collections.Look(ref _trackers, "HoldTrackers", LookMode.Deep);
                Scribe_Collections.Look<Pawn, Tracker>(ref _assignedTrackers, "assignedTrackers", LookMode.Reference, LookMode.Reference);
            }
        }
        #endregion Override Methods

        #region Methods
        /// <summary>
        /// Creates a new default loadout (there is only one default, the Empty loadout).
        /// </summary>
        /// <returns>Loadout which is the Empty Loadout.</returns>
        private static Loadout MakeDefaultLoadout()
        {
            return new Loadout("CE_EmptyLoadoutName".Translate(), 1) { canBeDeleted = false, defaultLoadout = true };
        }

        /// <summary>
        /// Returns a List of HoldRecords for the Pawn.
        /// </summary>
        /// <param name="pawn">Pawn to get the List for.</param>
        /// <returns>List of HoldRecords or null if the pawn has none.</returns>
        public static List<HoldRecord> GetHoldRecords(Pawn pawn) // Rename Try?
        {
            if (_current == null) return null;
            Tracker tracker;
            if (_current._assignedTrackers.TryGetValue(pawn, out tracker))
                return tracker.recs;
            return null;
        }

        /// <summary>
        /// Utility to clean up HoldTracker entries for pawns which are dead or who no longer have HoldRecords.  Useful pre-save.
        /// </summary>
        public static void PurgeHoldTrackerRolls()
        {
            if (_current == null) return;
            List<Pawn> removeList = new List<Pawn>(_current._assignedTrackers.Keys.Count);
            foreach (Pawn pawn in _current._assignedTrackers.Keys)
            {
                if (pawn.Dead)
                    removeList.Add(pawn); // remove dead pawns from the rolls
                else if (!_current._assignedTrackers[pawn].recs.Any())
                    removeList.Add(pawn); // remove pawns with no HoldRecords stored.
                else if (pawn.DestroyedOrNull())
                    removeList.Add(pawn); // remove pawns have been destroyed or are null.
            }
            foreach (Pawn pawn in removeList)
            {
                Tracker remTracker = _current._assignedTrackers[pawn];
                _current._trackers.Remove(remTracker);
                _current._assignedTrackers.Remove(pawn);
            }
        }

        /// <summary>
        /// Similar to the PurgeHoldTrackerRolls, clean up loadout data, mostly used pre-save but can be used elsewhere.
        /// </summary>
        public static void PurgeLoadoutRolls()
        {
            if (_current == null) return;
            List<Pawn> removeList = new List<Pawn>(_current._assignedLoadouts.Keys.Count);
            foreach (Pawn pawn in _current._assignedLoadouts.Keys)
            {
                if (pawn.Dead)
                    removeList.Add(pawn);   // remove dead pawns from the rolls, they should become null pawns on game save.
                else if (pawn.DestroyedOrNull())
                    removeList.Add(pawn);   // remove pawns that have been destroyed or are null.
            }
            foreach (Pawn pawn in removeList)
                _current._assignedLoadouts.Remove(pawn);
        }

        /// <summary>
        /// Adds a List of HoldRecord to the indicated Pawn.
        /// </summary>
        /// <param name="pawn">Pawn for whome new List should be stored.</param>
        /// <param name="newRecords">List of HoldRecord that should be attached to pawn.</param>
        public static void AddHoldRecords(Pawn pawn, List<HoldRecord> newRecords)
        {
            if (_current == null) return;
            Tracker tracker = new Tracker(newRecords);
            _current._trackers.Add(tracker);
            _current._assignedTrackers.Add(pawn, tracker);
        }

        public static void AddLoadout(Loadout loadout)
        {
            if (_current == null) return;
            _current._loadouts.Add(loadout);
        }

        public static void RemoveLoadout(Loadout loadout)
        {
            if (_current == null) return;
            // assign default loadout to pawns that used to use this loadout
            List<Pawn> obsolete = AssignedLoadouts.Where(kvp => kvp.Value == loadout).Select(kvp => kvp.Key).ToList(); // ToList separates this from the dictionary, ienumerable in this case would break as we change the relationship.
            foreach (Pawn id in obsolete)
                AssignedLoadouts[id] = DefaultLoadout;

            _current._loadouts.Remove(loadout);
        }

        /// <summary>
        /// Used to ensure that future retrievals of loadouts are sorted.  Doesn't need to be called often, just right before fetching and only when it matters.
        /// </summary>
        public static void SortLoadouts()
        {
            if (_current == null) return;
            _current._loadouts.Sort();
        }

        /// <summary>
        /// Used turring instanciation of new Tracker objects.
        /// </summary>
        /// <returns></returns>
        internal static int GetUniqueTrackerID()
        {
            LoadoutManager manager = Current.Game.GetComponent<LoadoutManager>();
            if (manager != null && manager._assignedTrackers.Values.Any())
                return manager._assignedTrackers.Values.Max(t => t.uniqueID) + 1;
            else
                return 1;
        }

        internal static int GetUniqueLoadoutID()
        {
            LoadoutManager manager = Current.Game.GetComponent<LoadoutManager>();
            if (manager != null && manager._loadouts.Any())
                return manager._loadouts.Max(l => l.uniqueID) + 1;
            else
                return 1;
        }

        internal static string GetUniqueLabel()
        {
            return GetUniqueLabel("CE_DefaultLoadoutName".Translate());
        }

        internal static bool IsUniqueLabel(string label)
        {
            LoadoutManager manager = Current.Game.GetComponent<LoadoutManager>();
            // For consistency with the 'GetUniqueLabel' behavior
            if (manager == null)
            {
                return false;
            }
            return !manager._loadouts.Any(l => l.label == label);
        }

        internal static string GetUniqueLabel(string head)
        {
            LoadoutManager manager = Current.Game.GetComponent<LoadoutManager>();
            string label;
            int i = 1;
            if (manager != null)
            {
                do
                {
                    label = head + i++;
                }
                while (manager._loadouts.Any(l => l.label == label));
            } else
                label = head + i++;
            return label;
        }

        internal static Loadout GetLoadoutById(int id)
        {
            if (_current == null) return null;
            return Loadouts.Find(x => x.uniqueID == id);
        }
        #endregion Methods
    }
}