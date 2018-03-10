using System.Collections.Generic;
using System.Reflection;
using Harmony;
using Verse;

namespace CombatExtended.Harmony
{
    [HarmonyPatch(typeof(ThingListGroupHelper), "Includes")]
    internal static class Harmony_ThingListGroupHelper
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.MethodReplacer(typeof(ThingDef).GetMethod("get_IsShell"),
                typeof(AmmoUtility).GetMethod(nameof(AmmoUtility.IsShell), BindingFlags.Public | BindingFlags.Static));
        }
    }
}