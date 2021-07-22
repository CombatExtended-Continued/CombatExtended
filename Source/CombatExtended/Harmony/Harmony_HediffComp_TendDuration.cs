using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace CombatExtended.HarmonyCE
{
    /* Targetting Verse.HediffComp_TendDuration.CompTended
	 * Specifically the (decompiled) line this.tendQuality = Mathf.Clamp01(quality + Rand.Range(-0.25f, 0.25f));
	 * More specifically the value -0.25f and 0.25f
	 */
    [HarmonyPatch(typeof(HediffComp_TendDuration))] // Target class for patching, generally required.
    [HarmonyPatch(nameof(HediffComp_TendDuration.CompTended))] // Target method for patching, generally required.
    [HarmonyPatch(new Type[] { typeof(float), typeof(float), typeof(int) })] // Target method signature (arguments), not generally required if there is only one method with the target name in the target class.
    static class HediffComp_TendDuration_CompTended
    {
        // can name the method something else but then requires the attribute [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int countReplace = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                if (countReplace < 2 && instruction.opcode == OpCodes.Ldc_R4 && (instruction.OperandIs(-0.25f) || instruction.OperandIs(0.25f)))
                {
                    instruction.operand = countReplace == 0 ? -0.15f : 0.15f;
                    countReplace++;
                }
                yield return instruction;
            }
        }
    }
}
