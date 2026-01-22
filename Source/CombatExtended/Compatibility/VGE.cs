using System.Collections.Generic;
using Verse;

namespace CombatExtended.Compatibility;

public class VGE : IPatch
{
    const string ModName = "Vanilla Gravship Expanded - Chapter 1";
    bool IPatch.CanInstall()
    {
        Log.Message("Combat Extended :: Checking VGE");
        if (!ModLister.HasActiveModWithName(ModName))
        {
            return false;
        }
        return true;
    }

    public void Install()
    {
        Log.Message("Combat Extended :: Installing VGE");
    }
}
