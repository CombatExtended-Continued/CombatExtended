using RimWorld;
using Verse;

namespace CombatExtended
{
	/// <summary>
	/// HoldRecord stores key information for an individual element of HoldTracker.
	/// </summary>
	/// <remarks>Wow this is looking a lot like a LoadoutSlot now...  Remembering by Thing doesn't work out.</remarks>
	public class HoldRecord : IExposable
	{
		#region Fields
		const int INVALIDTICKS = GenDate.TicksPerDay;
		
		private ThingDef _def;
		public int count;
		public bool pickedUp;
		private int _tickJobIssued;
		
		#endregion
		
		#region Constructors
		/// <summary>
		/// Constructor for when a pawn was instructed to pick something up but hasn't yet, ie HoldTracker was notified.  By default pickedUp will be false.
		/// </summary>
		/// <param name="newThingDef">The ThingDef to add to HoldTracker.</param>
		/// <param name="newCount">How much to hold onto.</param>
		public HoldRecord(ThingDef newThingDef, int newCount)
		{
			_def = newThingDef;
			count = newCount;
			pickedUp = false;
			_tickJobIssued = GenTicks.TicksAbs;
		}

		/// <summary>
		/// Default Constructor.  Used by Rimworld Load/Save and shouldn't be used by any other code.
		/// </summary>
		public HoldRecord()
		{
		}
		#endregion
		
		#region Properties
		/// <summary>
		/// If the item hasn't been picked up is it still valid to remember?
		/// </summary>
		public virtual bool isTimeValid	{ get { return !pickedUp && ((GenTicks.TicksAbs - _tickJobIssued) <= INVALIDTICKS); } }
		
		public virtual ThingDef thingDef { get { return this._def; } }
		
		#endregion
		
		#region Methods
		/// <summary>
		/// Meant to be used in debug messages, attempt to capture the state of the object if a tad wordy.
		/// </summary>
		/// <returns>string containing debug message.</returns>
		public override string ToString()
		{
			return string.Concat("HoldRecord for ", thingDef, " of count ", count, " which has", (!pickedUp ? "n't" : ""), " been picked up",
			                           (!pickedUp ? string.Concat(" with a job issue time of ", (GenTicks.TicksAbs - _tickJobIssued),
			                                                      " ago and will go invalid in ", (GenTicks.TicksAbs + INVALIDTICKS - _tickJobIssued)) : "."));
		}
        #endregion

        #region IExposable implementation

        /// <summary>
        /// RimWorld Load/Save handler.
        /// </summary>
        public virtual void ExposeData()
        {
            Scribe_Defs.Look(ref _def, "ThingDef");
            Scribe_Values.Look(ref count, "count");
            Scribe_Values.Look(ref pickedUp, "pickedUp");
            if (!pickedUp)
            {
                Scribe_Values.Look(ref _tickJobIssued, "tickOfPickupJob");
            }
        }
            #endregion
    }
}