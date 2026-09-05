using HarmonyLib;
using VEF.Apparels;

namespace CombatExtended.Compatibility.VEFCompat;

[HarmonyPatch(typeof(VanillaExpandedFramework_ThingDef_SpecialDisplayStats_Postfix_Patch.SetFaction), nameof(VanillaExpandedFramework_ThingDef_SpecialDisplayStats_Postfix_Patch.SetFaction.Postfix))]
public class Harmony_RemoveUsableWIthShields
{
    public static bool Prefix()
    {
        return false;
    }
}
