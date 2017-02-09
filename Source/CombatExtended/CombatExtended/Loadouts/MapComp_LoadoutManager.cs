using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class LoadoutManager : MapComponent
    {
        #region Fields
        private static LoadoutManager _instance;
        private Dictionary<Pawn, Loadout> _assignedLoadouts = new Dictionary<Pawn, Loadout>();
        private List<LoadoutAssignment> _assignedLoadoutsScribeHelper = new List<LoadoutAssignment>();
        private List<Loadout> _loadouts = new List<Loadout>();

        #endregion Fields

        #region Constructors

        public LoadoutManager(Map map) : base(map)
        {
            // create a default empty loadout
            // there needs to be at least one default tagged loadout at all times
            _loadouts.Add( new Loadout( "CE_EmptyLoadoutName".Translate(), 1 ) { canBeDeleted = false, defaultLoadout = true } );
        }

        #endregion Constructors

        #region Properties

        public static LoadoutManager Instance
        {
            get
            {
                Map map = Find.VisibleMap;
                if ( _instance == null )
                    _instance = new LoadoutManager(map);
                return _instance;
            }
        }

        public static Dictionary<Pawn, Loadout> AssignedLoadouts { get { return Instance._assignedLoadouts; } }

        public static Loadout DefaultLoadout { get { return Instance._loadouts.First( l => l.defaultLoadout ); } }

        public static List<Loadout> Loadouts { get { return Instance._loadouts; } }

        #endregion Properties

        #region Methods

        public static void AddLoadout( Loadout loadout )
        {
            Instance._loadouts.Add( loadout );
        }

        public static void RemoveLoadout( Loadout loadout )
        {
            Instance._loadouts.Remove( loadout );

            // assign default loadout to pawns that used to use this loadout
            IEnumerable<Pawn> obsolete = AssignedLoadouts.Where( a => a.Value == loadout ).Select( a => a.Key );
            foreach ( Pawn id in obsolete )
            {
                AssignedLoadouts[id] = DefaultLoadout;
            }
        }

        public override void ExposeData()
        {
            ////// List of pawns that are out of the map
            ////List<string> pawnsOutOfMap_IDs = new List<string>();

            // scribe available loadouts
            Scribe_Collections.LookList<Loadout>( ref Instance._loadouts, "loadouts", LookMode.Deep );

            //scribe loadout assignments (for some reason using the dictionary directly doesn't work -- Fluffy)
            // create list of scribe helper objects
            if ( Scribe.mode == LoadSaveMode.Saving )
                Instance._assignedLoadoutsScribeHelper = Instance._assignedLoadouts.Select(pair => new LoadoutAssignment() { pawn = pair.Key, loadout = pair.Value }).ToList();

            //scribe that list
            Scribe_Collections.LookList( ref Instance._assignedLoadoutsScribeHelper, "assignments", LookMode.Deep );


            ////if (Scribe.mode == LoadSaveMode.LoadingVars)
            ////{
            ////    //Test if any pawns are out of the map; if yes, remove them from loadout assignments
            ////    // TO DO: verify if they retain their loadouts after they come back on the map!
            ////    List<Thing> lstPawnsOutOfMap = new List<Thing>();
            ////    Scribe_Collections.LookList(ref lstPawnsOutOfMap, "PawnsOutOfMap", LookMode.Deep);

            ////    if (lstPawnsOutOfMap != null && lstPawnsOutOfMap.Count > 0)
            ////    {
            ////        foreach (Thing th in lstPawnsOutOfMap)
            ////        {
            ////            Pawn p = th as Pawn;
            ////            if (p != null)
            ////            {
            ////                pawnsOutOfMap_IDs.Add(p.GetUniqueLoadID());
            ////            }
            ////        }

            ////        lstPawnsOutOfMap = null;
            ////    }
            ////    else
            ////    {
            ////        lstPawnsOutOfMap = null;
            ////        pawnsOutOfMap_IDs = null;
            ////    }
            ////}

            // convert back into useable dictionary
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                // removes assignments that for some reason have a null value.
                IEnumerable<LoadoutAssignment> temp = Instance._assignedLoadoutsScribeHelper
                    .Where(a => (a.Valid == true) && (!a.pawn.Dead) && (!a.pawn.DestroyedOrNull()) );

                ////// removes assignments for colonists not on the map!
                ////// TO DO: test if their loadouts get reassigned when they come back on the map!
                ////if (pawnsOutOfMap_IDs != null && pawnsOutOfMap_IDs.Count > 0)
                ////{
                ////    temp = temp.Where(x => (x != null) && !(pawnsOutOfMap_IDs.Contains(x.pawn.GetUniqueLoadID())) );
                ////}

                Instance._assignedLoadouts = temp.ToDictionary(k => k.pawn, v => v.loadout );
            }
        }

        internal static int GetUniqueID()
        {
            if ( Loadouts.Any() )
                return Loadouts.Max( l => l.uniqueID ) + 1;
            else
                return 1;
        }

        internal static string GetUniqueLabel()
        {
            string label;
            int i = 1;
            do
            {
                label = "CE_DefaultLoadoutName".Translate() + i++;
            }
            while ( Loadouts.Any( l => l.label == label ) );
            return label;
        }

        internal static Loadout GetLoadoutById(int id)
        {
            return Loadouts.Find(x => x.uniqueID == id);
        }

        #endregion Methods
    }
}