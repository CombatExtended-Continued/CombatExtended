using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using Harmony;
using Verse;
using RimWorld;

namespace CombatExtended.Harmony
{
    [HarmonyPatch(typeof(Fire), "DoFireDamage")]
    internal static class Harmony_Fire_DoFireDamage
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach(CodeInstruction code in instructions)
            {
                if (code.opcode == OpCodes.Ldc_R4 && code.operand is float && (float)code.operand == 150f)
                {
                    code.operand = 300f;
                }
                yield return code;
            }
        }
    }

    [HarmonyPatch(typeof(Fire), "Tick")]
    internal static class Harmony_Fire_Tick
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.opcode == OpCodes.Ldc_R4 && code.operand is float && (float)code.operand == 1f)
                {
                    code.operand = 0.1f;
                }
                yield return code;
            }
        }
    }

    [HarmonyPatch(typeof(Fire), "get_SpreadInterval")]
    internal static class Harmony_Fire_SpreadInterval
    {
        private const float BaseSpreadRate = 31.3f;
        private const float SpreadSizeAdjust = 28.3f;
        private const int MinSpreadTicks = 10;

        internal static bool Prefix(Fire __instance, ref float __result)
        {
            __result = BaseSpreadRate - (__instance.fireSize - 1) * SpreadSizeAdjust;
            if (__result > MinSpreadTicks)
            {
                __result = MinSpreadTicks;
            }

            return false;
        }
    }
}
