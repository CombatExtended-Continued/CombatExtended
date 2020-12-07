using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace CombatExtended.Utilities
{
    public static class GenClosest
    {
        public static ThingTracker GetThingTracker(this Map map)
        {
            return ThingTracker.GetTracker(map);
        }

        public static IEnumerable<Thing> SimilarInRange(this Thing thing, float range)
        {
            ThingTracker tracker = thing.Map.GetThingTracker();
            return tracker.SimilarInRangeOf(thing, range);
        }

        public static IEnumerable<Thing> ThingsByDefInRange(this IntVec3 cell, Map map, ThingDef thingDef, float range)
        {
            ThingTracker tracker = map.GetThingTracker();
            return tracker.ThingsInRangeOf(thingDef, cell, range);
        }

        public static IEnumerable<Pawn> PawnsInRange(this IntVec3 cell, Map map, float range)
        {
            ThingTracker tracker = map.GetThingTracker();
            return tracker.ThingsInRangeOf(null, cell, range).Select(t => t as Pawn);
        }
    }
}
