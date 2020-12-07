using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace CombatExtended.Utilities
{
    public static class GenClosest
    {
        public static ThingsTracker GetThingTracker(this Map map)
        {
            return ThingsTracker.GetTracker(map);
        }

        public static IEnumerable<Thing> SimilarInRange(this Thing thing, float range)
        {
            ThingsTracker tracker = thing.Map.GetThingTracker();
            return tracker.SimilarInRangeOf(thing, range);
        }

        public static IEnumerable<Thing> ThingsByDefInRange(this IntVec3 cell, Map map, ThingDef thingDef, float range)
        {
            ThingsTracker tracker = map.GetThingTracker();
            return tracker.ThingsInRangeOf(thingDef, cell, range);
        }

        public static IEnumerable<Pawn> PawnsInRange(this IntVec3 cell, Map map, float range)
        {
            ThingsTracker tracker = map.GetThingTracker();
            return tracker.ThingsInRangeOf(null, cell, range).Select(t => t as Pawn);
        }

        public static IEnumerable<Thing> SimilarInRange(this Thing thing, float range, PathEndMode pathEndMode = PathEndMode.None, TraverseMode traverseMode = TraverseMode.ByPawn, Danger danger = Danger.Unspecified)
        {
            ThingsTracker tracker = thing.Map.GetThingTracker();
            return ThingsReachableFrom(thing.Map, tracker.SimilarInRangeOf(thing, range), thing, pathEndMode, traverseMode, danger);
        }

        public static IEnumerable<Thing> ThingsByDefInRange(this IntVec3 cell, Map map, ThingDef thingDef, float range, PathEndMode pathEndMode = PathEndMode.None, TraverseMode traverseMode = TraverseMode.ByPawn, Danger danger = Danger.Unspecified)
        {
            ThingsTracker tracker = map.GetThingTracker();
            return ThingsReachableFrom(map, tracker.ThingsInRangeOf(thingDef, cell, range), cell, pathEndMode, traverseMode, danger);
        }

        public static IEnumerable<Pawn> PawnsInRange(this IntVec3 cell, Map map, float range, PathEndMode pathEndMode = PathEndMode.None, TraverseMode traverseMode = TraverseMode.ByPawn, Danger danger = Danger.Unspecified)
        {
            ThingsTracker tracker = map.GetThingTracker();
            return ThingsReachableFrom(map, tracker.ThingsInRangeOf(null, cell, range), cell, pathEndMode, traverseMode, danger).Select(t => t as Pawn);
        }

        private static IEnumerable<Thing> ThingsReachableFrom(Map map, IEnumerable<Thing> things, IntVec3 position, PathEndMode pathEndMode, TraverseMode traverseMode, Danger danger = Danger.Unspecified)
        {
            foreach (Thing t in things)
                if (map.reachability.CanReach(position, t, pathEndMode, traverseMode, danger))
                    yield return t;
        }

        private static IEnumerable<Thing> ThingsReachableFrom(Map map, IEnumerable<Thing> things, Thing thing, PathEndMode pathEndMode, TraverseMode traverseMode, Danger danger = Danger.Unspecified)
        {
            IntVec3 position = thing.Position;
            foreach (Thing t in things)
                if (map.reachability.CanReach(position, t, pathEndMode, traverseMode, danger))
                    yield return thing;
        }
    }
}
