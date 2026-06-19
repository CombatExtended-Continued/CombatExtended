using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;
using Verse;

namespace CombatExtended;
/// <summary>
/// Owns the player-defined custom loadout groups. Groups are global (shared across all saves): they
/// are persisted to a single config file, registered into <see cref="DefDatabase{LoadoutGenericDef}"/>
/// at startup, and added/removed live so edits take effect without a restart.
/// </summary>
/// <remarks>
/// Deletion uses a tombstone rather than removing the def from the database (RimWorld has no public
/// removal): the def is hidden from <see cref="AllGroups"/> and references are stripped immediately,
/// and because it is no longer written to the config it is simply not re-registered on next launch.
/// </remarks>
[StaticConstructorOnStartup]
public static class CustomLoadoutGroupManager
{
    private const string _fileName = "CombatExtended_CustomLoadoutGroups.xml";
    private const string _defNamePrefix = "CE_CustomGroup_";

    private static readonly HashSet<ListLoadoutGenericDef> _tombstoned = new();
    private static bool _loaded;

    private static string ConfigFilePath => Path.Combine(GenFilePaths.ConfigFolderPath, _fileName);

    /// <summary>All live (non-deleted) custom groups currently registered.</summary>
    public static IEnumerable<ListLoadoutGenericDef> AllGroups =>
        DefDatabase<LoadoutGenericDef>.AllDefs.OfType<ListLoadoutGenericDef>().Where(g => !_tombstoned.Contains(g));

    #region Startup registration

    /// <summary>
    /// Reads the global config and registers every custom group as a <see cref="ListLoadoutGenericDef"/>.
    /// Called once from <see cref="LoadoutGenericDef"/>'s static constructor, after the built-in generics
    /// exist so groups may nest them.
    /// </summary>
    public static void LoadAndRegisterAll()
    {
        if (_loaded)
        {
            return;
        }
        _loaded = true;
        try
        {
            CustomGroupConfig[] configs = ReadConfig().groups;
            if (configs.NullOrEmpty())
            {
                return;
            }

            HashSet<ushort> takenHashes = SeedTakenHashes();
            var byDefName = new Dictionary<string, CustomGroupConfig>();
            var created = new List<ListLoadoutGenericDef>();

            // Pass 1: create + register empty defs so nested references resolve regardless of file order.
            foreach (CustomGroupConfig cfg in configs)
            {
                if (cfg.defName.NullOrEmpty() || byDefName.ContainsKey(cfg.defName)
                    || DefDatabase<LoadoutGenericDef>.GetNamedSilentFail(cfg.defName) != null)
                {
                    continue;
                }
                byDefName[cfg.defName] = cfg;
                var def = new ListLoadoutGenericDef
                {
                    defName = cfg.defName,
                    label = cfg.label,
                    defaultCount = cfg.defaultCount,
                    defaultCountType = cfg.defaultCountType,
                };
                RegisterDef(def, takenHashes);
                created.Add(def);
            }
            DefDatabase<LoadoutGenericDef>.InitializeShortHashDictionary();

            // Pass 2: resolve members now that all group defs are present.
            foreach (ListLoadoutGenericDef def in created)
            {
                PopulateMembers(def, byDefName[def.defName]);
                def.RebuildLambda();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Combat Extended :: Failed to load custom loadout groups: {e}");
        }
    }

    #endregion Startup registration

    #region Mutations (hot)

    /// <summary>
    /// Creates an unregistered draft group for the editor. It is not added to the database or config
    /// until <see cref="Commit"/> is called, so it stays invisible (and unsaved) until the user confirms.
    /// </summary>
    public static ListLoadoutGenericDef CreateDraft() => new()
    {
        label = NewUniqueLabel(),
        defaultCount = 1,
        defaultCountType = LoadoutCountType.pickupDrop,
    };

    /// <summary>Registers a draft from <see cref="CreateDraft"/> into the database and persists it.</summary>
    public static void Commit(ListLoadoutGenericDef draft)
    {
        draft.defName = NewUniqueDefName();
        RegisterDef(draft, SeedTakenHashes());
        DefDatabase<LoadoutGenericDef>.InitializeShortHashDictionary();
        draft.RebuildLambda();
        SaveAll();
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
        def.SetMembers(source.things, source.groups);
        RegisterDef(def, SeedTakenHashes());
        DefDatabase<LoadoutGenericDef>.InitializeShortHashDictionary();
        SaveAll();
        return def;
    }

    public static void Delete(ListLoadoutGenericDef group)
    {
        _tombstoned.Add(group);

        foreach (ListLoadoutGenericDef other in AllGroups)
        {
            other.RemoveGroup(group);
        }
        if (LoadoutManager.Loadouts != null)
        {
            foreach (Loadout loadout in LoadoutManager.Loadouts)
            {
                loadout.OwnSlots.RemoveAll(s => s.genericDef == group);
            }
        }
        SaveAll();
    }

    public static void Rename(ListLoadoutGenericDef group, string label)
    {
        group.label = label;
        SaveAll();
    }

    /// <summary>Persists the current set of groups to the global config file.</summary>
    public static void Save() => SaveAll();

    /// <summary>Adds a member (thing or nested group). Returns false (without modifying) if it would cycle.</summary>
    public static bool AddMember(ListLoadoutGenericDef group, Def member)
    {
        bool changed;
        if (member is ThingDef thing)
        {
            changed = group.AddThing(thing);
        }
        else if (member is LoadoutGenericDef nested)
        {
            if (WouldCreateCycle(group, nested))
            {
                return false;
            }
            changed = group.AddGroup(nested);
        }
        else
        {
            return true;
        }
        if (changed)
        {
            SaveAll();
        }
        return true;
    }

    public static void RemoveMember(ListLoadoutGenericDef group, Def member)
    {
        bool changed = member switch
        {
            ThingDef thing => group.RemoveThing(thing),
            LoadoutGenericDef nested => group.RemoveGroup(nested),
            _ => false,
        };
        if (changed)
        {
            SaveAll();
        }
    }

    /// <summary>
    /// Reorders <paramref name="member"/> within its own list. <paramref name="displayToIndex"/> indexes
    /// the combined things-then-groups display and is clamped to the member's own section.
    /// </summary>
    public static void MoveMember(ListLoadoutGenericDef group, Def member, int displayToIndex)
    {
        if (member is ThingDef thing)
        {
            int to = Mathf.Clamp(displayToIndex, 0, group.things.Count - 1);
            group.MoveThing(group.things.IndexOf(thing), to);
            SaveAll();
        }
        else if (member is LoadoutGenericDef nested)
        {
            int to = Mathf.Clamp(displayToIndex - group.things.Count, 0, group.groups.Count - 1);
            group.MoveGroup(group.groups.IndexOf(nested), to);
            SaveAll();
        }
    }

    /// <summary>True if nesting <paramref name="candidate"/> into <paramref name="container"/> would cycle.</summary>
    public static bool WouldCreateCycle(ListLoadoutGenericDef container, LoadoutGenericDef candidate) =>
        ReferenceGraph.WouldCreateCycle((LoadoutGenericDef)container, candidate, NestedGroupsOf);

    // Graph edges for cycle detection: a list group points at the groups it nests; anything else is a leaf.
    private static IEnumerable<LoadoutGenericDef> NestedGroupsOf(LoadoutGenericDef def) =>
        def is ListLoadoutGenericDef list ? list.groups : Enumerable.Empty<LoadoutGenericDef>();

    #endregion Mutations (hot)

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
        var things = new List<ThingDef>();
        var groups = new List<LoadoutGenericDef>();
        foreach (string name in cfg.thingDefNames ?? Array.Empty<string>())
        {
            if (name.NullOrEmpty())
            {
                continue;
            }
            ThingDef thing = DefDatabase<ThingDef>.GetNamedSilentFail(name);
            if (thing == null)
            {
                Log.Warning($"Combat Extended :: Custom loadout group '{def.defName}' references missing item '{name}'; dropping it.");
                continue;
            }
            things.Add(thing);
        }
        foreach (string name in cfg.groupDefNames ?? Array.Empty<string>())
        {
            if (name.NullOrEmpty())
            {
                continue;
            }
            LoadoutGenericDef nested = DefDatabase<LoadoutGenericDef>.GetNamedSilentFail(name);
            if (nested == null)
            {
                Log.Warning($"Combat Extended :: Custom loadout group '{def.defName}' references missing group '{name}'; dropping it.");
                continue;
            }
            groups.Add(nested);
        }
        def.SetMembers(things, groups);
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
        var existing = new HashSet<string>(AllGroups.Select(g => g.label));
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

    private static CustomGroupConfigList ReadConfig()
    {
        string path = ConfigFilePath;
        if (!File.Exists(path))
        {
            return new CustomGroupConfigList();
        }
        try
        {
            var serializer = new XmlSerializer(typeof(CustomGroupConfigList));
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            return (CustomGroupConfigList)serializer.Deserialize(stream) ?? new CustomGroupConfigList();
        }
        catch (Exception e)
        {
            Log.Error($"Combat Extended :: Failed to read custom loadout groups from '{path}': {e}");
            return new CustomGroupConfigList();
        }
    }

    private static void SaveAll()
    {
        var list = new CustomGroupConfigList
        {
            groups = AllGroups.Select(ToConfig).ToArray(),
        };
        try
        {
            var serializer = new XmlSerializer(typeof(CustomGroupConfigList));
            using var writer = new StreamWriter(ConfigFilePath);
            serializer.Serialize(writer, list);
        }
        catch (Exception e)
        {
            Log.Error($"Combat Extended :: Failed to save custom loadout groups: {e}");
        }
    }

    private static CustomGroupConfig ToConfig(ListLoadoutGenericDef group) => new()
    {
        defName = group.defName,
        label = group.label,
        defaultCount = group.defaultCount,
        defaultCountType = group.defaultCountType,
        thingDefNames = group.things.Select(t => t.defName).ToArray(),
        groupDefNames = group.groups.Select(g => g.defName).ToArray(),
    };

    #endregion Helpers
}
