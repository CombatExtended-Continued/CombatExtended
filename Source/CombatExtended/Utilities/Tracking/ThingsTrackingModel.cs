using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.PerformanceData;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using Verse;

namespace CombatExtended.Utilities
{
    public class ThingsTrackingModel
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct ThingPositionInfo : IComparable<ThingPositionInfo>
        {
            public Thing thing;
            public int createdOn;

            public int Age
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => GenTicks.TicksGame - createdOn;
            }

            public bool IsValid
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => (thing?.Spawned ?? false) && thing.positionInt.InBounds(thing.Map);
            }

            public ThingPositionInfo(Thing thing)
            {
                this.thing = thing;
                this.createdOn = GenTicks.TicksGame;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode()
            {
                return thing.thingIDNumber;
            }

            public override bool Equals(object obj)
            {
                return obj.GetHashCode() == GetHashCode();
            }

            public override string ToString()
            {
                return $"{thing.ToString()}:{thing.Position.ToString()}:{createdOn.ToString()}";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int CompareTo(ThingPositionInfo other)
            {
                return thing.Position.x.CompareTo(other.thing.Position.x);
            }

            public static bool operator >(ThingPositionInfo operand1, ThingPositionInfo operand2)
            {
                return operand1.CompareTo(operand2) == 1;
            }

            public static bool operator <(ThingPositionInfo operand1, ThingPositionInfo operand2)
            {
                return operand1.CompareTo(operand2) == -1;
            }

            public static bool operator >=(ThingPositionInfo operand1, ThingPositionInfo operand2)
            {
                return operand1.CompareTo(operand2) >= 0;
            }

            public static bool operator <=(ThingPositionInfo operand1, ThingPositionInfo operand2)
            {
                return operand1.CompareTo(operand2) <= 0;
            }
        }

        public readonly ThingDef def;
        public readonly ThingsTracker parent;
        public readonly Map map;

        private int count = 0;
        private Dictionary<Thing, int> indexByThing = new Dictionary<Thing, int>();
        private ThingPositionInfo[] sortedThings = new ThingPositionInfo[100];

        public ThingsTrackingModel(ThingDef def, Map map, ThingsTracker parent)
        {
            this.def = def;
            this.map = map;
            this.parent = parent;
        }

        public void Register(Thing thing)
        {
            if (indexByThing.ContainsKey(thing))
                return;
            if (count + 1 >= sortedThings.Length)
            {
                ThingPositionInfo[] temp = new ThingPositionInfo[sortedThings.Length * 2];
                for (int i = 0; i < sortedThings.Length; i++)
                    temp[i] = sortedThings[i];
                sortedThings = temp;
            }

            sortedThings[count] = new ThingPositionInfo(thing);
            indexByThing[thing] = count;
            count++;

            int index = count - 1;
            while (index - 1 >= 0 && sortedThings[index] < sortedThings[index - 1])
            {
                Swap<ThingPositionInfo>(index - 1, index, sortedThings);
                indexByThing[sortedThings[index].thing] = index;
                indexByThing[sortedThings[index - 1].thing] = index - 1;
                index--;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DeRegister(Thing thing)
        {
            if (indexByThing.TryGetValue(thing, out int index))
            {
                RemoveClean(index);
                indexByThing.Remove(thing);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clean() => SweepClean();

        public void Notify_ThingPositionChanged(Thing thing)
        {
            if (!indexByThing.TryGetValue(thing, out int index))
            {
                SweepClean();
                Register(thing);
            }
            else
            {
                int i = index;
                while (i + 1 < count && sortedThings[i] > sortedThings[i + 1])
                {
                    Swap<ThingPositionInfo>(i + 1, i, sortedThings);
                    indexByThing[sortedThings[i].thing] = i;
                    indexByThing[sortedThings[i + 1].thing] = i + 1;
                    i++;
                }
                i = index;
                while (i - 1 >= 0 && sortedThings[i] < sortedThings[i - 1])
                {
                    Swap<ThingPositionInfo>(i - 1, i, sortedThings);
                    indexByThing[sortedThings[i].thing] = i;
                    indexByThing[sortedThings[i - 1].thing] = i - 1;
                    i--;
                }
            }
        }

        public IEnumerable<Thing> ThingsInRangeOf(IntVec3 cell, float range)
        {
            int bottom = 0;
            int index;
            int top = count;
            int mid = (top + bottom) / 2;
            int limiter = 0;
            IntVec3 midPosition;
            while (top != bottom && limiter++ < 20)
            {
                mid = (top + bottom) / 2;
                midPosition = sortedThings[mid].thing.Position;
                if (midPosition.DistanceTo(cell) <= range)
                    break;
                if (midPosition.x > cell.x)
                    top = mid - 1;
                else if (midPosition.x < cell.x)
                    bottom = mid + 1;
                else
                {
                    break;
                }
            }
            index = mid;
            while (index < count)
            {
                Thing t = sortedThings[index].thing;
                IntVec3 curPosition = t.Position;
                if (Mathf.Abs(curPosition.x - cell.x) > range)
                    break;
                if (curPosition.DistanceTo(cell) <= range)
                    yield return t;
                index++;
            }
            index = mid - 1;
            while (index >= 0)
            {
                Thing t = sortedThings[index].thing;
                IntVec3 curPosition = t.Position;
                if (Mathf.Abs(curPosition.x - cell.x) > range)
                    break;
                if (curPosition.DistanceTo(cell) <= range)
                    yield return t;
                index--;
            }
        }

        private void RemoveClean(int index)
        {
            int i;
            int offset = 1;
            for (i = index + 1; i < count && i - offset >= 0; i++)
            {
                ThingPositionInfo current = sortedThings[i];
                if (!current.IsValid)
                {
                    offset++;
                    if (indexByThing.ContainsKey(current.thing))
                        indexByThing.Remove(current.thing);
                    current.thing = null;
                }
                else if (offset > 0)
                {
                    indexByThing[current.thing] = i - offset;
                    sortedThings[i - offset] = current;
                }
            }
            for (i = count - offset; i < count; i++)
                sortedThings[i].thing = null;
            count -= offset;
        }

        private void SweepClean()
        {
            int i = 0;
            int offset = 0;
            for (; i < count && i - offset >= 0; i++)
            {
                ThingPositionInfo current = sortedThings[i];
                if (!current.IsValid)
                {
                    offset++;
                    if (indexByThing.ContainsKey(current.thing))
                        indexByThing.Remove(current.thing);
                    current.thing = null;
                }
                else if (offset > 0)
                {
                    indexByThing[current.thing] = i - offset;
                    sortedThings[i - offset] = current;
                }
            }
            for (i = count - offset; i < count; i++)
                sortedThings[i].thing = null;
            count -= offset;
            if (Prefs.DevMode) Log.Message($"Combat Extended: cleaned model for {def.defName}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap<T>(int a, int b, T[] list)
        {
            T temp = list[a];
            list[a] = list[b];
            list[b] = temp;
        }
    }
}
