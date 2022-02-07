using System;

namespace CombatExtended
{
    // Loadout 'config' file structure (i.e. reusable configurations that can be saved and reloaded into other games)

    [Serializable]
    public class LoadoutConfig
    {
        public string label;
        public LoadoutSlotConfig[] slots;
    }
    
    [Serializable]
    public class LoadoutSlotConfig
    {
        public bool isGenericDef;
        public string defName;
        public int count;
    }
    
}