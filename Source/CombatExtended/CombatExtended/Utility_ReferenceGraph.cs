using System;
using System.Collections.Generic;

namespace CombatExtended;
/// <summary>
/// Generic directed-graph helpers for keeping reference relationships acyclic when adding edges.
/// Edges are supplied lazily through a neighbor selector, so no graph is materialized. Reusable
/// anywhere an acyclic reference graph is needed (custom loadout groups, modular loadout parents, ...).
/// </summary>
public static class ReferenceGraph
{
    /// <summary>
    /// Every node reachable from <paramref name="start"/> by following <paramref name="getNeighbors"/>,
    /// each yielded once. <paramref name="start"/> itself is yielded only if it lies on a cycle.
    /// </summary>
    public static IEnumerable<T> EnumerateReachable<T>(T start, Func<T, IEnumerable<T>> getNeighbors, IEqualityComparer<T> comparer = null)
    {
        comparer ??= EqualityComparer<T>.Default;
        var visited = new HashSet<T>(comparer);
        var stack = new Stack<T>();
        stack.Push(start);
        while (stack.Count > 0)
        {
            IEnumerable<T> neighbors = getNeighbors(stack.Pop());
            if (neighbors == null)
            {
                continue;
            }
            foreach (T neighbor in neighbors)
            {
                if (visited.Add(neighbor))
                {
                    stack.Push(neighbor);
                    yield return neighbor;
                }
            }
        }
    }

    /// <summary>True if <paramref name="target"/> equals or is reachable from <paramref name="start"/>.</summary>
    public static bool IsReachable<T>(T start, T target, Func<T, IEnumerable<T>> getNeighbors, IEqualityComparer<T> comparer = null)
    {
        comparer ??= EqualityComparer<T>.Default;
        if (comparer.Equals(start, target))
        {
            return true;
        }
        foreach (T node in EnumerateReachable(start, getNeighbors, comparer))
        {
            if (comparer.Equals(node, target))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// True if adding the edge <paramref name="from"/> → <paramref name="to"/> would create a cycle:
    /// either they are the same node, or <paramref name="from"/> is already reachable from <paramref name="to"/>.
    /// </summary>
    public static bool WouldCreateCycle<T>(T from, T to, Func<T, IEnumerable<T>> getNeighbors, IEqualityComparer<T> comparer = null)
    {
        comparer ??= EqualityComparer<T>.Default;
        return comparer.Equals(from, to) || IsReachable(to, from, getNeighbors, comparer);
    }
}
