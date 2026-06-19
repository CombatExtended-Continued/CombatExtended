using System;
using System.Collections.Generic;
using Verse;

namespace CombatExtended;
// Serializable form of a custom loadout group. Used two ways: scribed into the save (per-game storage,
// via IExposable) and exported/imported to a standalone file (via XmlSerializer, like LoadoutConfig).

[Serializable]
public class CustomGroupConfig : IExposable
{
    public string defName;
    public string label;
    public int defaultCount = 1;
    public LoadoutCountType defaultCountType = LoadoutCountType.pickupDrop;
    public List<CustomGroupMember> members = new();

    public void ExposeData()
    {
        Scribe_Values.Look(ref defName, "defName");
        Scribe_Values.Look(ref label, "label");
        Scribe_Values.Look(ref defaultCount, "defaultCount", 1);
        Scribe_Values.Look(ref defaultCountType, "defaultCountType", LoadoutCountType.pickupDrop);
        Scribe_Collections.Look(ref members, "members", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            members ??= new List<CustomGroupMember>();
        }
    }
}

// One ordered member of a custom group: a concrete item or a nested group, tracked by defName.
[Serializable]
public class CustomGroupMember : IExposable
{
    public string defName;
    public bool isGroup;

    public CustomGroupMember()
    {
    }

    public CustomGroupMember(string defName, bool isGroup)
    {
        this.defName = defName;
        this.isGroup = isGroup;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref defName, "defName");
        Scribe_Values.Look(ref isGroup, "isGroup");
    }
}
