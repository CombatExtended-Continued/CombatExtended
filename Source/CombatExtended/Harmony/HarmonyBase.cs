using System.Reflection;
using Harmony;
using Verse;

namespace CombatRealism.Harmony
{
	[StaticConstructorOnStartup]
	static class HarmonyBase
	{
		private static HarmonyInstance harmony = null;
		
		static HarmonyBase()
		{
			// Unremark the following when developing new Harmony patches.  The file "harmony.log.txt" on your desktop and is always appended.  Will cause ALL patches to be debugged.
			//HarmonyInstance.DEBUG = true;
			
			// The following line will cause all properly formatted and annotated classes to patch the target code.
			instance.PatchAll(Assembly.GetExecutingAssembly());
			
			// NOTE: Technically one shouldn't mix PatchAll() and Patch() but I didn't get a clear understanding of how/if this was bad or just not a good idea.
		}
		
		/// <summary>
		/// Fetch CombatExtended's instance of Harmony.
		/// </summary>
		/// <remarks>One should only have a single instance of Harmony per Assembly.</remarks>
		static internal HarmonyInstance instance
		{
			get 
			{
				if (harmony == null)
					harmony = harmony = HarmonyInstance.Create("CombatExtended.Harmony");
				return harmony;
			}
		}
	}
}
