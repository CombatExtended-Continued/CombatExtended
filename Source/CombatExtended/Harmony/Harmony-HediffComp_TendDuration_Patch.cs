//using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using Verse;

namespace CombatExtended.Harmony
{
	/* Targetting Verse.HediffComp_TendDuration.CompTended
	 * Specifically the (decompiled) line this.tendQuality = Mathf.Clamp01(quality + Rand.Range(-0.25f, 0.25f));
	 * More specifically the value -0.25f and 0.25f
	 */ 
	[HarmonyPatch(typeof(HediffComp_TendDuration))]
	[HarmonyPatch("CompTended")]
	static class HediffComp_TendDuration_Patch
	{
		
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> CompTended_Patch(IEnumerable<CodeInstruction> instructions)
		{
			int countReplace = 0;
			foreach (CodeInstruction instruction in instructions)
			{
				if (countReplace < 2 && instruction.opcode == OpCodes.Ldc_R4)
				{
					instruction.operand = countReplace == 0 ? -0.15f : 0.15f;
					countReplace++;
				}
				yield return instruction;
			}
		}
	}
}
