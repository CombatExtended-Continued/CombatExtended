using RimWorld;
using Verse;

namespace CombatExtended
{
	/// <summary>
	/// HoldRecord stores key information for an individual element of HoldTracker.
	/// </summary>
	/// <remarks>Wow this is looking a lot like a LoadoutSlot now...  Remembering by Thing doesn't work out.</remarks>
	public class HoldRecord
	{
		#region Fields
		const int INVALIDTICKS = GenDate.TicksPerDay;
		
		private ThingDef def;
		public int count;
		public bool pickedUp;
		private readonly int tickJobIssued;
		
		#endregion
		
		#region Constructors
		/// <summary>
		/// Constructor for when a pawn was instructed to pick something up but hasn't yet, ie HoldTracker was notified.  By default pickedUp will be false.
		/// </summary>
		/// <param name="newThingDef">The ThingDef to add to HoldTracker.</param>
		/// <param name="newCount">How much to hold onto.</param>
		public HoldRecord(ThingDef newThingDef, int newCount)
		{
			def = newThingDef;
			count = newCount;
			pickedUp = false;
			tickJobIssued = GenTicks.TicksAbs;
		}
		#endregion
		
		#region Properties
		/// <summary>
		/// If the item hasn't been picked up is it still valid to remember?
		/// </summary>
		public bool isTimeValid	{ get { return !pickedUp && ((GenTicks.TicksAbs - tickJobIssued) <= INVALIDTICKS); } }
		
		public ThingDef thingDef { get { return this.def; } }
		
		#endregion
		
		#region Methods
		/// <summary>
		/// Meant to be used in debug messages, attempt to capture the state of the object if a tad wordy.
		/// </summary>
		/// <returns>string containing debug message.</returns>
		public override string ToString()
		{
			return string.Concat("HoldRecord for ", thingDef, " of count ", count, " which has", (!pickedUp ? "n't" : ""), " been picked up",
			                           (!pickedUp ? string.Concat(" with a job issue time of ", (GenTicks.TicksAbs - tickJobIssued),
			                                                      " ago and will go invalid in ", (GenTicks.TicksAbs + INVALIDTICKS - tickJobIssued)) : "."));
		}
		#endregion
	}
}