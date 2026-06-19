using System.Collections.Generic;
using Verse;

namespace CombatExtended;
/// <summary>
/// A player-defined <see cref="LoadoutGenericDef"/> whose match set is the union of a list of concrete
/// <see cref="ThingDef"/>s and a list of nested <see cref="LoadoutGenericDef"/>s (built-in generics or
/// other custom groups, enabling nesting). Built and registered at runtime by
/// <see cref="CustomLoadoutGroupManager"/> from the global config.
/// </summary>
public class ListLoadoutGenericDef : LoadoutGenericDef
{
    public List<ThingDef> things = new();
    public List<LoadoutGenericDef> groups = new();
    // Mirrors `things` for O(1) membership tests in the match predicate; kept in sync by the mutators.
    private readonly HashSet<ThingDef> _thingSet = new();

    public ListLoadoutGenericDef()
    {
        _lambda = MatchesAny;
    }

    public bool AddThing(ThingDef thing)
    {
        if (thing == null || !_thingSet.Add(thing))
        {
            return false;
        }
        things.Add(thing);
        RebuildLambda();
        return true;
    }

    public bool RemoveThing(ThingDef thing)
    {
        if (!_thingSet.Remove(thing))
        {
            return false;
        }
        things.Remove(thing);
        RebuildLambda();
        return true;
    }

    public bool AddGroup(LoadoutGenericDef group)
    {
        if (group == null || groups.Contains(group))
        {
            return false;
        }
        groups.Add(group);
        RebuildLambda();
        return true;
    }

    public bool RemoveGroup(LoadoutGenericDef group)
    {
        if (!groups.Remove(group))
        {
            return false;
        }
        RebuildLambda();
        return true;
    }

    // Reordering does not change the match set, so the lambda/cache stay valid.
    public void MoveThing(int fromIndex, int toIndex) => Move(things, fromIndex, toIndex);

    public void MoveGroup(int fromIndex, int toIndex) => Move(groups, fromIndex, toIndex);

    /// <summary>Replaces all members at once (config load or edit revert) and resyncs caches.</summary>
    public void SetMembers(IEnumerable<ThingDef> newThings, IEnumerable<LoadoutGenericDef> newGroups)
    {
        things.Clear();
        things.AddRange(newThings);
        groups.Clear();
        groups.AddRange(newGroups);
        _thingSet.Clear();
        _thingSet.UnionWith(things);
        RebuildLambda();
    }

    /// <summary>Re-affirms the match predicate and invalidates cached bulk/mass. Call after editing members.</summary>
    public void RebuildLambda()
    {
        _lambda = MatchesAny;
        InvalidateStatCache();
    }

    private bool MatchesAny(ThingDef td)
    {
        if (td == null)
        {
            return false;
        }
        if (_thingSet.Contains(td))
        {
            return true;
        }
        foreach (var t in groups) {
            if (t.lambda(td))
            {
                return true;
            }
        }
        return false;
    }

    private static void Move<T>(List<T> list, int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= list.Count || toIndex < 0 || toIndex >= list.Count || fromIndex == toIndex)
        {
            return;
        }
        T item = list[fromIndex];
        list.RemoveAt(fromIndex);
        if (fromIndex + 1 < toIndex)
        {
            toIndex--;
        }
        list.Insert(toIndex, item);
    }
}
