using HarmonyLib;
using Verse;

namespace CombatExtended.Compatibility.WeaponProficiency.HarmonyPatches
{
    [StaticConstructorOnStartup]
    public static class HarmonyStarter
    {
        static HarmonyStarter()
        {
            Harmony harmony = new Harmony("CombatExtended.Compatibility.WeaponProficiency");
            harmony.PatchAll();
        }
    }
}
