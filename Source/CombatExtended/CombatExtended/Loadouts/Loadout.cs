using System.Text.RegularExpressions;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace CombatExtended
{
	/// <summary>
	/// Contains a series of LoadoutSlot slots which define what a pawn using this loadout should try to keep in their inventory.
	/// </summary>
    public class Loadout : IExposable, ILoadReferenceable, IComparable
    {
        #region Fields

        public bool canBeDeleted = true;
        public bool defaultLoadout = false; //NOTE: assumed that there is only ever one loadout which is marked default.
        public string label;
        internal int uniqueID;
        private List<LoadoutSlot> _slots = new List<LoadoutSlot>();

        #endregion Fields

        #region Constructors

        public Loadout()
        {
            // this constructor is also used by the scribe, in which case defaults generated here will get overwritten.

            // create a unique default name.
            label = LoadoutManager.GetUniqueLabel();

            // create a unique ID.
            uniqueID = LoadoutManager.GetUniqueLoadoutID();
        }
        
        public Loadout(string label)
        {
            this.label = label;

            // create a unique ID.
            uniqueID = LoadoutManager.GetUniqueLoadoutID();
        }

        /// <summary>
        /// CAUTION: This constructor allows setting the uniqueID and goes unchecked for collisions.
        /// </summary>
        /// <param name="label">string label of new Loadout, preferably unique but not required.</param>
        /// <param name="uniqueID">int ID of new Loadout.  This should be unique to avoid bugs.</param>
        public Loadout(string label, int uniqueID)
        {
            this.label = label;
            this.uniqueID = uniqueID;
        }
        
        /// <summary>
        /// Handles adding any LoadoutGenercDef as LoadoutSlots if they are flagged as basic.
        /// </summary>
        public void AddBasicSlots()
        {
        	IEnumerable<LoadoutGenericDef> defs = DefDatabase<LoadoutGenericDef>.AllDefs.Where(d => d.isBasic);
        	foreach (LoadoutGenericDef def in defs)
        	{
        		LoadoutSlot slot = new LoadoutSlot(def);
        		AddSlot(slot);
        	}
        }

        #endregion Constructors

        #region Properties

        public float Bulk { get { return _slots.Sum(slot => slot.bulk * slot.count); } }
        public string LabelCap { get { return label.CapitalizeFirst(); } }
        public int SlotCount { get { return _slots.Count; } }
        public List<LoadoutSlot> Slots { get { return _slots; } }
        public float Weight { get { return _slots.Sum(slot => slot.mass * slot.count); } }

        #endregion Properties

        #region Methods
        
        // Returns a copy of this loadout slot with a new unique ID and a label based on the original name.
		// LoadoutSlots need to be copied.     
		/// <summary>
		/// Handles copying one Loadout to a new Loadout object.
		/// </summary>
		/// <param name="source">Loadout from which to copy properties/fields from.</param>
		/// <returns>new Loadout with copied properties from 'source'</returns>
		/// <remarks>
		/// uniqueID will be different as required.
		/// label will be different as required, but related to original.
		/// Slots are copied (not the same object) but have the same properties as source.Slots.
		/// </remarks>
        static Loadout Copy(Loadout source)
        {
        	string newName = source.label;
        	Regex reNum = new Regex(@"^(.*?)\d+$");
        	if (reNum.IsMatch(newName))
        		newName = reNum.Replace(newName, @"$1");
        	newName = LoadoutManager.GetUniqueLabel(newName);
        	
        	Loadout dest = new Loadout(newName);
        	dest.defaultLoadout = source.defaultLoadout;
        	dest.canBeDeleted = source.canBeDeleted;
        	dest._slots = new List<LoadoutSlot>();
        	foreach(LoadoutSlot slot in source.Slots)
        		dest.AddSlot(slot.Copy());
        	return dest;
        }
        
        /// <summary>
        /// Copies self to a new Loadout.  <see cref="Copy(Loadout)"/>.
        /// </summary>
        /// <returns>new Loadout with copied properties from self.</returns>
        public Loadout Copy()
        {
        	return Copy(this);
        }

        /// <summary>
        /// Handles adding a new LoadoutSlot to self.  If self already has the same slot (based on Def) then increment the already present slot.count.
        /// </summary>
        /// <param name="slot">LoadoutSlot to add to this Loadout.</param>
        public void AddSlot(LoadoutSlot slot)
        {
        	LoadoutSlot old = _slots.FirstOrDefault(slot.isSameDef);
            if (old != null)
        		old.count += slot.count;
        	else
            	_slots.Add(slot);
        }

        /// <summary>
        /// Handles the save/load process as part of IExplosable.
        /// </summary>
        public void ExposeData()
        {
            // basic info about this loadout
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref uniqueID, "uniqueID");
            Scribe_Values.Look(ref canBeDeleted, "canBeDeleted", true);
            Scribe_Values.Look(ref defaultLoadout, "defaultLoadout", false);

            // slots
            Scribe_Collections.Look(ref _slots, "slots", LookMode.Deep);
        }

        public string GetUniqueLoadID()
        {
            return "Loadout_" + label + "_" + uniqueID;
        }

        /// <summary>
        /// Used to move a slot in this Loadout to a different position in the List.
        /// </summary>
        /// <param name="slot">LoadoutSlot being moved.</param>
        /// <param name="toIndex">int position (index) to move slot to.</param>
        public void MoveSlot(LoadoutSlot slot, int toIndex)
        {
            int fromIndex = _slots.IndexOf(slot);
            MoveTo(fromIndex, toIndex);
        }

        /// <summary>
        /// Used to remove a LoadoutSlot from this Loadout.
        /// </summary>
        /// <param name="slot">LoadoutSlot to remove.</param>
        public void RemoveSlot(LoadoutSlot slot)
        {
            _slots.Remove(slot);
        }

        /// <summary>
        /// Used to remove a LoadoutSlot by index from this Loadout.  Usually used when moving slots around (ie drag and drop).
        /// </summary>
        /// <param name="index">int index of this Loadout's Slot List to remove.</param>
        public void RemoveSlot(int index)
        {
            _slots.RemoveAt(index);
        }

        /// <summary>
        /// Used to move one LoadoutSlot into a different position in this Loadout's List.  Generally connected to drag and drop activities by user.
        /// </summary>
        /// <param name="fromIndex">int index (source) in List to move from.</param>
        /// <param name="toIndex">int index (target) in List to move to.</param>
        /// <returns></returns>
        private int MoveTo(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _slots.Count || toIndex < 0 || toIndex >= _slots.Count)
            {
                throw new Exception("Attempted to move i " + fromIndex + " to " + toIndex + ", bounds are [0," + (_slots.Count - 1) + "].");
            }

            // fetch the filter we're moving
            LoadoutSlot temp = _slots[fromIndex];

            // remove from old location
            _slots.RemoveAt(fromIndex);

            // this may have changed the toIndex
            if (fromIndex + 1 < toIndex)
                toIndex--;

            // insert at new location
            _slots.Insert(toIndex, temp);
            return toIndex;
        }

        #endregion Methods

		#region IComparable implementation

		/// <summary>
		/// Used when sorting lists of Loadouts.
		/// </summary>
		/// <param name="obj">other object to compare to.</param>
		/// <returns>int -1 indicating this is before obj, 0 indicates this is the same as obj, 1 indicates this is after obj.</returns>
		public int CompareTo(object obj)
		{
			Loadout other = obj as Loadout;
			if (other == null)
				throw new ArgumentException("Loadout.CompareTo(obj), obj is not of type Loadout.");
			
			// initial case, default comes first.  Currently there aren't more than one default and should never be.
			if (this.defaultLoadout && other.defaultLoadout) return 0;
			if (this.defaultLoadout) return -1;
			if (other.defaultLoadout) return 1;
			
			// now we just compare by name of loadout...
			return this.label.CompareTo(other.label);
		}

		#endregion
    }
}