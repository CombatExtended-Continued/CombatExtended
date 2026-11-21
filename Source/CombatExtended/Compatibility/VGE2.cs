using Verse;

namespace CombatExtended.Compatibility;

public class VGE2 : IPatch
{
    const string ModName = "Vanilla Gravship Expanded - Chapter 2";
    bool IPatch.CanInstall()
    {
        Log.Message("Combat Extended :: Checking VGE2");
        if (!ModLister.HasActiveModWithName(ModName))
        {
            return false;
        }
        return true;
    }

    public void Install()
    {
        Log.Message("Combat Extended :: Installing VGE2");
    }
}
