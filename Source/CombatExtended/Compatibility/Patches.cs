using Verse;
  
namespace CombatExtended.Compatibility
{
    class Patches
    {
	public static void Init()
	{
	    if (EDShields.CanInstall())
	    {
		EDShields.Install();
	    }
	}
    }
}
