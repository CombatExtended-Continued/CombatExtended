using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(LordToil_Siege), "LordToilTick")]
    internal static class Harmony_LordToil_Siege
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.MethodReplacer(typeof(ThingDef).GetMethod("get_IsShell"),
                typeof(AmmoUtility).GetMethod(nameof(AmmoUtility.IsShell), BindingFlags.Public | BindingFlags.Static));
        }
    }
}
