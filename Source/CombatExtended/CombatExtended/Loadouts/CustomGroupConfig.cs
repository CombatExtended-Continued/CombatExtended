using System;

namespace CombatExtended;
// Serializable form of a player-defined custom loadout group, persisted globally (shared across saves)
// and reconstructed into ListLoadoutGenericDef instances at startup. Mirrors the LoadoutConfig pattern.

[Serializable]
public class CustomGroupConfig
{
    public string defName;
    public string label;
    public int defaultCount = 1;
    public LoadoutCountType defaultCountType = LoadoutCountType.pickupDrop;
    public string[] thingDefNames = Array.Empty<string>();
    public string[] groupDefNames = Array.Empty<string>();
}

[Serializable]
public class CustomGroupConfigList
{
    public CustomGroupConfig[] groups = Array.Empty<CustomGroupConfig>();
}
