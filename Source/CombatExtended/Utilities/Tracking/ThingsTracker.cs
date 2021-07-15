using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended.Utilities
{
    public class ThingsTracker : MapComponent
    {
        private static bool[] validDefs;

        private static Map[] maps = new Map[20];
        private static ThingsTracker[] comps = new ThingsTracker[20];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ThingsTracker GetTracker(Map map)
        {
            return map != null ? GetCachedTracker(map, fallbackMode: false) : null;
        }

        private static ThingsTracker GetCachedTracker(Map map, bool fallbackMode = false)
        {
            int index = map.Index;
            if (index >= 0 && index < comps.Length)
            {
                ThingsTracker tracker = comps[index];
                if (maps[index] != map)
                {
                    maps[index] = map;
                    comps[index] = tracker = map.GetComponent<ThingsTracker>();
                }
                return tracker;
            }
            else if (fallbackMode)
                throw new Exception($"GetTracker has failed twice for index {index}");
            Map[] tempMaps = new Map[Math.Max(maps.Length * 2, index + 1)];
            ThingsTracker[] tempComps = new ThingsTracker[Math.Max(comps.Length * 2, index + 1)];
            Array.Copy(maps, 0, tempMaps, 0, maps.Length);
            maps = tempMaps;
            Array.Copy(comps, 0, tempComps, 0, comps.Length);
            comps = tempComps;
            return GetCachedTracker(map, fallbackMode: true);
        }

        private ThingsTrackingModel[][] trackers;
        private ThingsTrackingModel pawnsTracker;
        private ThingsTrackingModel weaponsTracker;
        private ThingsTrackingModel apparelTracker;
        private ThingsTrackingModel ammoTracker;
        private ThingsTrackingModel medicineTracker;

        public ThingsTracker(Map map) : base(map)
        {
            pawnsTracker = new ThingsTrackingModel(null, map, this);
            weaponsTracker = new ThingsTrackingModel(null, map, this);
            ammoTracker = new ThingsTrackingModel(null, map, this);
            apparelTracker = new ThingsTrackingModel(null, map, this);
            medicineTracker = new ThingsTrackingModel(null, map, this);

            trackers = new ThingsTrackingModel[DefDatabase<ThingDef>.AllDefs.Max((def) => def.index) + 1][];
            for (int i = 0; i < trackers.Length; i++)
                trackers[i] = new ThingsTrackingModel[2];

            foreach (var def in DefDatabase<ThingDef>.AllDefs.Where(d => d.race != null || d.category == ThingCategory.Pawn))
                trackers[def.index][1] = pawnsTracker;
            foreach (var def in DefDatabase<ThingDef>.AllDefs.Where(d => d.IsWeapon))
                trackers[def.index][1] = weaponsTracker;
            foreach (var def in DefDatabase<ThingDef>.AllDefs.Where(d => d.IsApparel))
                trackers[def.index][1] = apparelTracker;
            foreach (var def in DefDatabase<ThingDef>.AllDefs.Where(d => d.IsMedicine))
                trackers[def.index][1] = medicineTracker;
            foreach (var def in DefDatabase<AmmoDef>.AllDefs)
                trackers[def.index][1] = ammoTracker;

            if (validDefs != null)
                return;

            validDefs = new bool[ushort.MaxValue];
            foreach (var def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def.category == ThingCategory.Mote)
                    validDefs[def.index] = false;
                else if (def.category == ThingCategory.Filth)
                    validDefs[def.index] = false;
                else if (def.category == ThingCategory.Building)
                    validDefs[def.index] = false;
                else if (def.category == ThingCategory.Gas)
                    validDefs[def.index] = false;
                else if (def.category == ThingCategory.Plant)
                    validDefs[def.index] = false;
                else
                    validDefs[def.index] = true;
            }
            foreach (var def in DefDatabase<FleckDef>.AllDefs)
                validDefs[def.index] = false;
        }

        public override void MapComponentOnGUI()
        {
            base.MapComponentOnGUI();
            if (!Controller.settings.DebugGenClosetPawn)
                return;
            if (Find.Selector.SelectedObjects.Count == 0)
                return;
            var thing = Find.Selector.SelectedObjects.Where(s => s is Thing).Select(s => s as Thing).First();
            if (IsValidTrackableThing(thing))
            {
                IEnumerable<Thing> others;
                if (thing is Pawn)
                    others = GenClosest.PawnsInRange(thing.Position, map, 50);
                else if (thing is AmmoThing)
                    others = GenClosest.AmmoInRange(thing.Position, map, 50);
                else if (thing.def.IsApparel)
                    others = GenClosest.ApparelInRange(thing.Position, map, 50);
                else if (thing.def.IsWeapon)
                    others = GenClosest.WeaponsInRange(thing.Position, map, 50);
                else
                    others = GenClosest.SimilarInRange(thing, 50);
                Vector2 a = UI.MapToUIPosition(thing.DrawPos);
                Vector2 b;
                Vector2 mid;
                Rect rect;
                int index = 0;
                foreach (var other in others)
                {
                    b = UI.MapToUIPosition(other.DrawPos);
                    Widgets.DrawLine(a, b, Color.red, 1);

                    mid = (a + b) / 2;
                    rect = new Rect(mid.x - 25, mid.y - 15, 50, 30);
                    Widgets.DrawBox(rect);
                    Widgets.Label(rect, $"({index++}). {other.Position.DistanceTo(thing.Position)}m");
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<Thing> SimilarInRangeOf(Thing thing, float range) => ThingsInRangeOf(thing.def, thing.Position, range);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<Thing> ThingsInRangeOf(TrackedThingsRequestCategory category, IntVec3 cell, float range)
        {
            ThingsTrackingModel tracker = GetModelFor(category);
            return tracker.ThingsInRangeOf(cell, range);
        }

        public IEnumerable<Thing> ThingsInRangeOf(ThingDef def, IntVec3 cell, float range)
        {
            if (!IsValidTrackableDef(def))
                throw new NotSupportedException();
            ThingsTrackingModel[] trackers = GetModelsFor(def);
            return trackers[0].ThingsInRangeOf(cell, range);
        }

        public void Register(Thing thing)
        {
            if (!IsValidTrackableThing(thing))
                return;
            ThingsTrackingModel[] trackers = GetModelsFor(thing);
            for (int i = 0; i < trackers.Length; i++)
                trackers[i]?.Register(thing);
        }

        public void Remove(Thing thing)
        {
            if (!IsValidTrackableThing(thing))
                return;
            ThingsTrackingModel[] trackers = GetModelsFor(thing);
            for (int i = 0; i < trackers.Length; i++)
                trackers[i]?.DeRegister(thing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ThingsTrackingModel[] GetModelsFor(Thing thing) => GetModelsFor(thing.def);

        public ThingsTrackingModel[] GetModelsFor(ThingDef def)
        {
            var result = trackers[def.index];
            if (result[0] != null)
                return result;
            result[0] = new ThingsTrackingModel(def, map, this);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ThingsTrackingModel GetModelFor(TrackedThingsRequestCategory category)
        {
            switch (category)
            {
                case TrackedThingsRequestCategory.Pawns:
                    return pawnsTracker;
                case TrackedThingsRequestCategory.Ammo:
                    return ammoTracker;
                case TrackedThingsRequestCategory.Apparel:
                    return apparelTracker;
                case TrackedThingsRequestCategory.Weapons:
                    return weaponsTracker;
                case TrackedThingsRequestCategory.Medicine:
                    return medicineTracker;
                default:
                    throw new NotSupportedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidTrackableThing(Thing thing) => IsValidTrackableDef(thing.def);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidTrackableDef(ThingDef def) => validDefs[def.index];

        public void Notify_Spawned(Thing thing)
        {
            if (!IsValidTrackableThing(thing))
                return;
            Register(thing);
        }

        public void Notify_DeSpawned(Thing thing)
        {
            if (!IsValidTrackableThing(thing))
                return;
            Remove(thing);
        }

        public void Notify_PositionChanged(Thing thing)
        {
            if (!IsValidTrackableThing(thing))
                return;
            ThingsTrackingModel[] trackers = GetModelsFor(thing.def);
            for (int i = 0; i < trackers.Length; i++)
                trackers[i]?.Notify_ThingPositionChanged(thing);
        }
    }
}
