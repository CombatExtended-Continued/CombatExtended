using Verse;

namespace CombatExtended
{
	public class Apparel_VisibleAccessoryDef : ThingDef
	{
		public int order = 1;
		private bool _Valid = false;
		
		public void validate()
		{
			if (order < 1 || order > 4)
			{
				int clamped;
				if (order < 1)
					clamped = 1;
				else
					clamped = 4;
				Log.Error(string.Concat(GetType().ToString(), " :: Order value ", order, " is out of bounds for Apparel '", label, "'.  Should be between 1 and 4 inclusive.  Value will be clamped to ", clamped, "."));
				order = clamped;
			}
			_Valid = true;
		}
		
		public bool isValid
		{
			get
			{
				return _Valid;
			}
		}
	}
}
