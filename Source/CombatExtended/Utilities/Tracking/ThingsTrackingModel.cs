using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CombatExtended.Utilities
{
    public class ThingsTrackingModel
    {
        private struct ThingPositionInfo : IComparable<ThingPositionInfo>
        {
            public Thing thing;
            public int createdOn;

            public bool IsValid => thing.Spawned;
            public int Age => GenTicks.TicksGame - createdOn;

            public ThingPositionInfo(Thing thing)
            {
                this.thing = thing;
                this.createdOn = GenTicks.TicksGame;
            }

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

        public Dictionary<Thing, int> indexByThing = new Dictionary<Thing, int>();

        private HashSet<Thing> things = new HashSet<Thing>();
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
            if (things.Count + 1 >= sortedThings.Length)
            {
                ThingPositionInfo[] temp = new ThingPositionInfo[sortedThings.Length * 2];
                for (int i = 0; i < sortedThings.Length; i++)
                    temp[i] = sortedThings[i];
                sortedThings = temp;
            }
            if (things.Count == 0)
            {
                sortedThings[things.Count] = new ThingPositionInfo(thing);
                indexByThing[thing] = 0;
                things.Add(thing);
                return;
            }
            else
            {
                sortedThings[things.Count] = new ThingPositionInfo(thing);
                indexByThing[thing] = things.Count - 1;
                things.Add(thing);

                int i = things.Count - 1;
                while (i - 1 >= 0 && sortedThings[i] < sortedThings[i - 1])
                {
                    Swap<ThingPositionInfo>(i - 1, i, sortedThings);
                    indexByThing[sortedThings[i].thing] = i;
                    indexByThing[sortedThings[i - 1].thing] = i - 1;
                    i--;
                }
            }
        }

        public void Remove(Thing thing)
        {
            if (!indexByThing.ContainsKey(thing))
                return;
            int index = indexByThing[thing];
            for (int i = index + 1; i < things.Count; i++)
            {
                indexByThing[sortedThings[i].thing] = i - 1;
                sortedThings[i - 1] = sortedThings[i];
            }
            indexByThing.Remove(thing);
            things.Remove(thing);
        }

        public void Notify_ThingPositionChanged(Thing thing)
        {
            int i;
            int index = indexByThing[thing];

            i = index;
            while (i + 1 < things.Count && sortedThings[i] > sortedThings[i + 1])
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

        public IEnumerable<Thing> ThingsInRangeOf(IntVec3 cell, float range)
        {
            int bottom = 0;
            int top = things.Count;
            int mid = (top + bottom) / 2;
            int index;
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
            while (index < things.Count)
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

        private static void Swap<T>(int a, int b, T[] list)
        {
            T temp = list[a];
            list[a] = list[b];
            list[b] = temp;
        }
    }
}
