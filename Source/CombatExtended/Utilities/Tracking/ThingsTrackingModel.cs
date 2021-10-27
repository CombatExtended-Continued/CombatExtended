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
                get => thing != null && thing.Spawned && thing.Position.InBounds(thing.Map);
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
            {
                Notify_ThingPositionChanged(thing);
                return;
            }
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

        public void Notify_ThingPositionChanged(Thing thing)
        {
            if (!indexByThing.TryGetValue(thing, out int index))
            {
                Register(thing);
                return;
            }
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


	/* Find all things near a given line segment.  For rays, the destination vector should be where the
	   ray intersects the edge of the map.  If behind is true, Things behind the origin (the ray moves away)
	   are excluded.
	 */
	public IEnumerable<Thing> ThingsNearSegment(IntVec3 origin, IntVec3 destination, float range, bool behind)
	{
	    float rangeSq = range * range;
	    float minX;
	    float maxX;
	    // Find the band of cells (1-D) the line segment spans.  The band is widened on both sides by range.
	    // ...<--range---x_min-----x_max---range-->...
	    // ...minX-----------------------------maxX...
	    if (origin.x > destination.x) {
		minX = destination.x - range;
		maxX = origin.x + range;
	    }
	    else {
		minX = origin.x - range;
		maxX = destination.x + range;
	    }

	    int bottom = 0;
            int index;
            int top = count;
            int mid = (top + bottom) / 2;
            int limiter = 0;
            IntVec3 midPosition;
	    IntVec3 direction = destination - origin;
	    float lengthSq = direction.x * direction.x + direction.y * direction.y + direction.z * direction.z;
	    if (lengthSq == 0) // origin and destination cell are the same, so just return all things within range radius of origin.
	    {
		foreach (Thing t in ThingsInRangeOf(origin, range))
		{
		    yield return t;
		}
		yield break;
	    }

	    while (top != bottom && limiter++ < 20) // try to find a good starting point for iterating nearby pawns
            {
		mid = (top + bottom) / 2;
                midPosition = sortedThings[mid].thing.Position;

		// Range of interest: ...minX-----------------------------maxX...
		// Possibility 1:     mP                                          false, true
		// Possibility 2:                                             mP  true, false
		// Possibility 3:                       mP                        true, true
		// Break when mP falls in the range
		if (midPosition.x > minX && midPosition.x < maxX)
		{
		    break;
		}
		if (midPosition.x > maxX)
		{
		    top = mid - 1;
		}
		else if (midPosition.x < minX)
		{
		    bottom = mid + 1;
		}
		else
		{
		    break;
		}
	    }

	    // mid is our best guess at a pawn that might be in the range.  
	    
	    index = mid;
            while (index < count) // take all the pawns from index to the right edge of the map
            {
                Thing t = sortedThings[index++].thing;
                IntVec3 curPosition = t.Position;
		// if we're outside the range of interest, we're done checking to the right.
                if (curPosition.x + range < minX || curPosition.x - range > maxX)  
		{
                    break;
		}

		IntVec3 relativePosition = curPosition - origin;
		
		// Calculate the dot product of the relativePosition (rp) vector with itself (the square of its magnitude)
		float rp_rp_dot = relativePosition.x * relativePosition.x + relativePosition.y * relativePosition.y + relativePosition.z * relativePosition.z;

		// Calculate the dot product of the direction vector and the relative displacement vector.  
		float rp_direction_dot = relativePosition.x * direction.x + relativePosition.y * direction.y + relativePosition.z * direction.z;

		// Calculate the ratio of the two dot products;
		float dot = rp_direction_dot / rp_rp_dot;
		if (dot > 1) // curPosition is beyond the end of the line segment.
		{
		    if (destination.DistanceToSquared(curPosition) <= rangeSq)
		    {
			yield return t;
			
		    }
		    continue;
		}
		if (dot < 0) // curPosition is behind the origin
		{
		    
		    if (behind && origin.DistanceToSquared(curPosition) <= rangeSq)
		    {
			yield return t;
		    }
		    continue;
		    
		}
		// dot ∈ (0,1) so curPosition is between the origin and destination
		IntVec3 projection = new IntVec3((int)(origin.x + dot * direction.x),
						 (int)(origin.y + dot * direction.y),
						 (int)(origin.z + dot * direction.z));
		if (projection.DistanceToSquared(curPosition) <= rangeSq)
		{
		    yield return t;
		}
            }
            index = mid - 1;
            while (index >= 0) // Same as above, but moving right.
            {
                Thing t = sortedThings[index--].thing;
		IntVec3 curPosition = t.Position;
                // if we're outside the range of interest, we're done checking to the right.
                if (curPosition.x + range < minX || curPosition.x - range > maxX)  
		{
                    break;
		}

		IntVec3 relativePosition = curPosition - origin;
		
		// Calculate the dot product of the relativePosition (rp) vector with itself (the square of its magnitude)
		float rp_rp_dot = relativePosition.x * relativePosition.x + relativePosition.y * relativePosition.y + relativePosition.z * relativePosition.z;

		// Calculate the dot product of the direction vector and the relative displacement vector.  
		float rp_direction_dot = relativePosition.x * direction.x + relativePosition.y * direction.y + relativePosition.z * direction.z;

		// Calculate the ratio of the two dot products;
		float dot = rp_direction_dot / rp_rp_dot;
		if (dot > 1) // curPosition is beyond the end of the line segment.
		{
		    if (destination.DistanceToSquared(curPosition) <= rangeSq)
		    {
			yield return t;
			
		    }
		    continue;
		}
		if (dot < 0) // curPosition is behind the origin
		{
		    
		    if (behind && origin.DistanceToSquared(curPosition) <= rangeSq)
		    {
			yield return t;
		    }
		    continue;
		    
		}
		// dot ∈ (0,1) so curPosition is between the origin and destination
		IntVec3 projection = new IntVec3((int)(origin.x + dot * direction.x),
						 (int)(origin.y + dot * direction.y),
						 (int)(origin.z + dot * direction.z));
		if (projection.DistanceToSquared(curPosition) <= rangeSq)
		{
		    yield return t;
		}
            }
	}

        public IEnumerable<Thing> ThingsInRangeOf(IntVec3 cell, float range)
        {
	    float rangeSq = range * range;
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
                if (midPosition.DistanceToSquared(cell) <= rangeSq)
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
                Thing t = sortedThings[index++].thing;
                IntVec3 curPosition = t.Position;
                if (Mathf.Abs(curPosition.x - cell.x) > range)
                    break;
                if (curPosition.DistanceToSquared(cell) <= rangeSq)
                    yield return t;
            }
            index = mid - 1;
            while (index >= 0)
            {
                Thing t = sortedThings[index--].thing;
                IntVec3 curPosition = t.Position;
                if (Mathf.Abs(curPosition.x - cell.x) > range)
                    break;
                if (curPosition.DistanceToSquared(cell) <= rangeSq)
                    yield return t;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap<T>(int a, int b, T[] list)
        {
            T temp = list[a];
            list[a] = list[b];
            list[b] = temp;
        }
    }
}
