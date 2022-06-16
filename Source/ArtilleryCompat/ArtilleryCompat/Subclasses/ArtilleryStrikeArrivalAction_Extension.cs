using VFESecurity;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace CombatExtended.Compatibility.Artillery
{
    public static class ArtilleryStrikeArrivalAction_Extension
    {
	public static void ArrivedCE(this ArtilleryStrikeArrivalAction_AIBase arrival, List<ActiveArtilleryStrike> artilleryStrikes, int tile)
	{
	    // Boom
	    if (arrival.CanDoArriveAction)
	    {
		var harmfulStrikes = Utility.HarmfulStrikes(artilleryStrikes);
		if (harmfulStrikes.Any())
		{
		    arrival.PreStrikeAction();
		    bool destroyed = false;
		    var mapRect = new CellRect(0, 0, arrival.MapSize, arrival.MapSize);
		    var baseRect = new CellRect(GenMath.RoundRandom(mapRect.Width / 2f) - GenMath.RoundRandom(arrival.BaseSize / 2f), GenMath.RoundRandom(mapRect.Height / 2f) - GenMath.RoundRandom(arrival.BaseSize / 2f), arrival.BaseSize, arrival.BaseSize);
		    for (int i = 0; i < harmfulStrikes.Count; i++)
		    {
			var strike = harmfulStrikes[i];
			for (int j = 0; j < strike.shellCount; j++)
			    arrival.StrikeAction(strike, mapRect, baseRect, ref destroyed);
		    }
		    arrival.PostStrikeAction(destroyed);
		}
	    }
	}
    }
}
