using System.Collections.Generic;
using System.Linq;
using Verse;

namespace CombatExtended;
/// <summary>
/// A player-defined <see cref="LoadoutGenericDef"/> whose match set is an ordered list of members,
/// each a concrete <see cref="ThingDef"/> or a nested <see cref="LoadoutGenericDef"/> (built-in
/// generic or another custom group, enabling nesting). Built and registered at runtime by
/// <see cref="CustomLoadoutGroupManager"/>.
/// </summary>
public class ListLoadoutGenericDef : LoadoutGenericDef
{
    /// <summary>
	/// Ordered members; each entry is a ThingDef or a LoadoutGenericDef.
	/// </summary>
    public List<Def> members = new();
    /// <summary>
	/// When <see langword="true"/>, rearming acquires members strictly in list order (earlier members satisfied first).
	/// </summary>
    public bool ordered;
    // Mirrors the ThingDef members for O(1) membership tests in the match predicate.
    private readonly HashSet<string> _thingSet = new();

    public ListLoadoutGenericDef()
    {
        _lambda = MatchesAny;
    }

    /// <summary>Nested groups only (for cycle detection); concrete things are leaves.</summary>
    public IEnumerable<LoadoutGenericDef> NestedGroups => members.OfType<LoadoutGenericDef>();

    public bool Add(ThingDef member)
    {
        if (_thingSet.Contains(member.defName))
        {
            return false;
        }
        members.Add(member);
        _thingSet.Add(member.defName);
        InvalidateStatCache();
        return true;
    }

    public bool Add(LoadoutGenericDef member)
    {
        if (NestedGroups.Any(g => ReferenceEquals(g, member)))
        {
            return false;
        }
        members.Add(member);
        InvalidateStatCache();
        return true;
    }

    public bool Remove(Def member)
    {
        if (!members.Remove(member))
        {
            return false;
        }
        if (member is ThingDef thing)
        {
            _thingSet.Remove(thing.defName);
        }
        InvalidateStatCache();
        return true;
    }

    // Reordering does not change the match set, so the lambda/cache stay valid.
    public void Move(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= members.Count || toIndex < 0 || toIndex >= members.Count || fromIndex == toIndex)
        {
            return;
        }
        Def def = members[fromIndex];
        members.RemoveAt(fromIndex);
        if (fromIndex + 1 < toIndex)
        {
            toIndex--;
        }
        members.Insert(toIndex, def);
    }

    /// <summary>Replaces all members at once (config load or edit revert) and resyncs caches.</summary>
    public void SetMembers(IEnumerable<Def> newMembers)
    {
        members.Clear();
        members.AddRange(newMembers);
        _thingSet.Clear();
        _thingSet.UnionWith(members.OfType<ThingDef>().Select(d => d.defName));
        InvalidateStatCache();
    }

    private bool MatchesAny(ThingDef td)
    {
        if (td == null)
        {
            return false;
        }
        if (_thingSet.Contains(td.defName))
        {
            return true;
        }
        foreach (Def member in members)
        {
            if (member is LoadoutGenericDef g && g.lambda(td))
            {
                return true;
            }
        }
        return false;
    }
}
