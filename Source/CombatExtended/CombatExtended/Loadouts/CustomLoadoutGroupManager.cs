using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace CombatExtended;
/// <summary>
/// Owns the custom loadout groups for the current game. Groups are stored per-save: their definitions
/// are scribed by <see cref="LoadoutManager"/> (which registers them before loadout slots resolve) and
/// rebuilt into <see cref="ListLoadoutGenericDef"/> instances on load. Editing mutates the live defs in
/// memory; the game save persists them. Groups can also be exported/imported to standalone files.
/// </summary>
/// <remarks>
/// Registered group defs linger in <see cref="DefDatabase{LoadoutGenericDef}"/> across game loads (RimWorld
/// has no public def removal), so on load a def with a saved name is reused and overwritten rather than
/// re-added. Only the groups in <see cref="_current"/> are live for the loaded game.
/// </remarks>
public static class CustomLoadoutGroupManager
{
    private const string _defNamePrefix = "CE_CustomGroup_";

    private static readonly List<ListLoadoutGenericDef> _current = new();

    /// <summary>All custom groups in the current game.</summary>
    public static IReadOnlyList<ListLoadoutGenericDef> AllGroups => _current;

    /// <summary>Clears the current game's groups (called when a game is started or loaded).</summary>
    public static void Reset() => _current.Clear();

    #region Save/load (per-game)

    /// <summary>
    /// Scribes the groups. Called from <see cref="LoadoutManager.ExposeData"/> before the loadouts, so the
    /// group defs are registered in time for loadout slots to resolve them by defName.
    /// </summary>
    public static void ExposeData()
    {
        List<CustomGroupConfig> configs = Scribe.mode == LoadSaveMode.Saving ? _current.Select(ToConfig).ToList() : null;
        Scribe_Collections.Look(ref configs, "customLoadoutGroups", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            Reset();
            RegisterFromConfigs(configs ?? new List<CustomGroupConfig>());
        }
    }

    private static void RegisterFromConfigs(List<CustomGroupConfig> configs)
    {
        HashSet<ushort> takenHashes = SeedTakenHashes();
        var byDefName = new Dictionary<string, CustomGroupConfig>();
        // pass 1: ensure a registered def exists for each group (reusing a lingering one by defName)
        foreach (CustomGroupConfig cfg in configs)
        {
            if (cfg.defName.NullOrEmpty() || byDefName.ContainsKey(cfg.defName))
            {
                continue;
            }
            byDefName[cfg.defName] = cfg;
            _current.Add(GetOrCreateDef(cfg, takenHashes));
        }
        DefDatabase<LoadoutGenericDef>.InitializeShortHashDictionary();
        // pass 2: resolve members now that every group def exists
        foreach (ListLoadoutGenericDef def in _current)
        {
            PopulateMembers(def, byDefName[def.defName]);
        }
    }

    private static ListLoadoutGenericDef GetOrCreateDef(CustomGroupConfig cfg, HashSet<ushort> takenHashes)
    {
        // a def with this name may linger from a previously-loaded game this session; reuse and overwrite it
        if (DefDatabase<LoadoutGenericDef>.GetNamedSilentFail(cfg.defName) is ListLoadoutGenericDef existing)
        {
            existing.label = cfg.label;
            existing.defaultCount = cfg.defaultCount;
            existing.defaultCountType = cfg.defaultCountType;
            existing.cachedLabelCap = "";
            return existing;
        }
        var def = new ListLoadoutGenericDef
        {
            defName = cfg.defName,
            label = cfg.label,
            defaultCount = cfg.defaultCount,
            defaultCountType = cfg.defaultCountType,
        };
        RegisterDef(def, takenHashes);
        return def;
    }

    #endregion Save/load (per-game)

    #region Mutations

    /// <summary>
    /// Creates an unregistered draft group for the editor. It is added to the game only by
    /// <see cref="Commit"/>, so it stays invisible until the user confirms.
    /// </summary>
    public static ListLoadoutGenericDef CreateDraft() => new()
    {
        label = NewUniqueLabel(),
        defaultCount = 1,
        defaultCountType = LoadoutCountType.pickupDrop,
    };

    /// <summary>Registers a draft from <see cref="CreateDraft"/> into the current game.</summary>
    public static void Commit(ListLoadoutGenericDef draft)
    {
        draft.defName = NewUniqueDefName();
        RegisterDef(draft, SeedTakenHashes());
        DefDatabase<LoadoutGenericDef>.InitializeShortHashDictionary();
        draft.RebuildLambda();
        _current.Add(draft);
    }

    public static ListLoadoutGenericDef Copy(ListLoadoutGenericDef source)
    {
        var def = new ListLoadoutGenericDef
        {
            defName = NewUniqueDefName(),
            label = NewUniqueLabel(source.label),
            defaultCount = source.defaultCount,
            defaultCountType = source.defaultCountType,
        };
        def.SetMembers(source.members);
        RegisterDef(def, SeedTakenHashes());
        DefDatabase<LoadoutGenericDef>.InitializeShortHashDictionary();
        _current.Add(def);
        return def;
    }

    public static void Delete(ListLoadoutGenericDef group)
    {
        _current.Remove(group);
        foreach (ListLoadoutGenericDef other in _current)
        {
            other.Remove(group);
        }
        if (LoadoutManager.Loadouts != null)
        {
            foreach (Loadout loadout in LoadoutManager.Loadouts)
            {
                loadout.OwnSlots.RemoveAll(s => s.genericDef == group);
            }
        }
    }

    /// <summary>Adds a member (thing or nested group). Returns false (without modifying) if it would cycle.</summary>
    public static bool AddMember(ListLoadoutGenericDef group, Def member)
    {
        if (member is LoadoutGenericDef nested && WouldCreateCycle(group, nested))
        {
            return false;
        }
        group.Add(member);
        return true;
    }

    public static void RemoveMember(ListLoadoutGenericDef group, Def member) => group.Remove(member);

    /// <summary>Reorders <paramref name="member"/> to <paramref name="toIndex"/> in the member list.</summary>
    public static void MoveMember(ListLoadoutGenericDef group, Def member, int toIndex) =>
        group.Move(group.members.IndexOf(member), Mathf.Clamp(toIndex, 0, group.members.Count - 1));

    /// <summary>True if nesting <paramref name="candidate"/> into <paramref name="container"/> would cycle.</summary>
    public static bool WouldCreateCycle(ListLoadoutGenericDef container, LoadoutGenericDef candidate) =>
        ReferenceGraph.WouldCreateCycle((LoadoutGenericDef)container, candidate, NestedGroupsOf);

    // Graph edges for cycle detection: a list group points at the groups it nests; anything else is a leaf.
    private static IEnumerable<LoadoutGenericDef> NestedGroupsOf(LoadoutGenericDef def) =>
        def is ListLoadoutGenericDef list ? list.NestedGroups : Enumerable.Empty<LoadoutGenericDef>();

    #endregion Mutations

    #region Export / import

    /// <summary>Serializable snapshot of a group, for save scribing and file export.</summary>
    public static CustomGroupConfig ToConfig(ListLoadoutGenericDef group) => new()
    {
        defName = group.defName,
        label = group.label,
        defaultCount = group.defaultCount,
        defaultCountType = group.defaultCountType,
        members = group.members.Select(m => new CustomGroupMember(m.defName, m is LoadoutGenericDef)).ToList(),
    };

    /// <summary>
    /// Loads an imported config's label and members into <paramref name="group"/>, resolving def names
    /// against the current game. Members that can't be resolved (or would create a cycle) are returned in
    /// <paramref name="unresolved"/>. The group's own identity (defName) is left unchanged.
    /// </summary>
    public static void ApplyConfig(ListLoadoutGenericDef group, CustomGroupConfig cfg, out List<string> unresolved)
    {
        unresolved = new List<string>();
        group.label = cfg.label;
        group.defaultCount = cfg.defaultCount;
        group.defaultCountType = cfg.defaultCountType;
        group.cachedLabelCap = "";

        var resolved = new List<Def>();
        foreach (CustomGroupMember entry in cfg.members ?? new List<CustomGroupMember>())
        {
            Def member = ResolveMember(entry);
            if (member == null || (member is LoadoutGenericDef nested && (nested == group || WouldCreateCycle(group, nested))))
            {
                unresolved.Add(entry?.defName);
                continue;
            }
            resolved.Add(member);
        }
        group.SetMembers(resolved);
    }

    #endregion Export / import

    #region Helpers

    private static void RegisterDef(ListLoadoutGenericDef def, HashSet<ushort> takenHashes)
    {
        // DefDatabase.Add doesn't resolve the name hash (only the XML/DefGenerator loaders do); without this
        // every runtime-registered group keeps defNameHash 0, so Def.Equals treats them all as the same def.
        def.ResolveDefNameHash();
        ShortHashGiver.GiveShortHash(def, typeof(LoadoutGenericDef), takenHashes);
        DefDatabase<LoadoutGenericDef>.Add(def);
    }

    private static void PopulateMembers(ListLoadoutGenericDef def, CustomGroupConfig cfg)
    {
        var resolved = new List<Def>();
        foreach (CustomGroupMember entry in cfg.members ?? new List<CustomGroupMember>())
        {
            if (entry == null || entry.defName.NullOrEmpty())
            {
                continue;
            }
            Def member = ResolveMember(entry);
            if (member == null)
            {
                Log.Warning($"Combat Extended :: Custom loadout group '{def.defName}' references missing {(entry.isGroup ? "group" : "item")} '{entry.defName}'; dropping it.");
                continue;
            }
            resolved.Add(member);
        }
        def.SetMembers(resolved);
    }

    // Resolves a stored member entry to its live def: groups from the generic database, items from the thing database.
    private static Def ResolveMember(CustomGroupMember entry)
    {
        if (entry == null || entry.defName.NullOrEmpty())
        {
            return null;
        }
        return entry.isGroup
               ? DefDatabase<LoadoutGenericDef>.GetNamedSilentFail(entry.defName)
               : DefDatabase<ThingDef>.GetNamedSilentFail(entry.defName);
    }

    private static HashSet<ushort> SeedTakenHashes() =>
        new(DefDatabase<LoadoutGenericDef>.AllDefs.Select(d => d.shortHash));

    private static string NewUniqueDefName()
    {
        int n = 1;
        string name;
        do
        {
            name = _defNamePrefix + n;
            n++;
        }
        while (DefDatabase<LoadoutGenericDef>.GetNamedSilentFail(name) != null);
        return name;
    }

    private static string NewUniqueLabel(string baseLabel = null)
    {
        string root = baseLabel.NullOrEmpty() ? (string)"CE_DefaultGroupLabel".Translate() : baseLabel;
        var existing = new HashSet<string>(_current.Select(g => g.label));
        if (!existing.Contains(root))
        {
            return root;
        }
        int n = 2;
        while (existing.Contains($"{root} {n}"))
        {
            n++;
        }
        return $"{root} {n}";
    }

    #endregion Helpers
}
