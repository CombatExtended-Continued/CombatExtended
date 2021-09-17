using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended.Utilities
{
    public static class GenClosest
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            return tracker.ThingsInRangeOf(TrackedThingsRequestCategory.Pawns, cell, range).Select(t => t as Pawn);
        }

        public static IEnumerable<Pawn> HostilesInRange(this IntVec3 cell, Map map, Faction faction, float range)
        {
            ThingsTracker tracker = map.GetThingTracker();
            return tracker.ThingsInRangeOf(TrackedThingsRequestCategory.Pawns, cell, range).Where(t => t.Faction?.HostileTo(faction) ?? false).Select(t => t as Pawn);
        }

        public static IEnumerable<AmmoThing> AmmoInRange(this IntVec3 cell, Map map, float range)
        {
            ThingsTracker tracker = map.GetThingTracker();
            return tracker.ThingsInRangeOf(TrackedThingsRequestCategory.Ammo, cell, range).Select(t => t as AmmoThing);
        }

        public static IEnumerable<ThingWithComps> WeaponsInRange(this IntVec3 cell, Map map, float range)
        {
            ThingsTracker tracker = map.GetThingTracker();
            return tracker.ThingsInRangeOf(TrackedThingsRequestCategory.Weapons, cell, range).Select(t => t as ThingWithComps);
        }

        public static IEnumerable<Thing> AttachmentsInRange(this IntVec3 cell, Map map, float range)
        {
            ThingsTracker tracker = map.GetThingTracker();
            return tracker.ThingsInRangeOf(TrackedThingsRequestCategory.Attachments, cell, range).Select(t => t as Thing);
        }

        public static IEnumerable<ThingWithComps> FlaresInRange(this IntVec3 cell, Map map, float range)
        {
            ThingsTracker tracker = map.GetThingTracker();
            return tracker.ThingsInRangeOf(TrackedThingsRequestCategory.Flares, cell, range).Select(t => t as ThingWithComps);
        }

        public static IEnumerable<ThingWithComps> MedicineInRange(this IntVec3 cell, Map map, float range)
        {
            ThingsTracker tracker = map.GetThingTracker();
            return tracker.ThingsInRangeOf(TrackedThingsRequestCategory.Medicine, cell, range).Select(t => t as ThingWithComps);
        }

        public static IEnumerable<Apparel> ApparelInRange(this IntVec3 cell, Map map, float range)
        {
            ThingsTracker tracker = map.GetThingTracker();
            return tracker.ThingsInRangeOf(TrackedThingsRequestCategory.Apparel, cell, range).Select(t => t as Apparel);
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
            return ThingsReachableFrom(map, tracker.ThingsInRangeOf(TrackedThingsRequestCategory.Pawns, cell, range), cell, pathEndMode, traverseMode, danger).Select(t => t as Pawn);
        }

        private static IEnumerable<Thing> ThingsReachableFrom(Map map, IEnumerable<Thing> things, IntVec3 position, PathEndMode pathEndMode, TraverseMode traverseMode, Danger danger = Danger.Unspecified)
        {
            return things.Where(t => map.reachability.CanReach(position, t, pathEndMode, traverseMode, danger));
        }

        private static IEnumerable<Thing> ThingsReachableFrom(Map map, IEnumerable<Thing> things, Thing thing, PathEndMode pathEndMode, TraverseMode traverseMode, Danger danger = Danger.Unspecified)
        {
            IntVec3 position = thing.Position;
            return things.Where(t => map.reachability.CanReach(position, t, pathEndMode, traverseMode, danger));
        }
    }
}
