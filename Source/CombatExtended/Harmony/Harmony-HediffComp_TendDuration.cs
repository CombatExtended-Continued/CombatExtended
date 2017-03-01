using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using Verse;

/*
namespace CombatExtended.Harmony
{
	[StaticConstructorOnStartup]
	static class Main
	{
		static Main()
		{
			var harmony = HarmonyInstance.Create("CombatExtended.Harmony.Pawn_HealthTracker");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}
		
		[HarmonyPatch(typeof(Pawn_HealthTracker))]
		[HarmonyPatch("CheckForStateChange")]
		static class Disable_RNG_Death
		{
			public class RNGReplacer : CodeProcessor
			{
				int countReplace = 0;
				
				public override List<CodeInstruction> Process(CodeInstruction instruction)
				{
					if (instruction == null) return null;
					Log.Message(string.Concat("Instruction: ", instruction.opcode.ToString(), " :: ", instruction.operand == null ? "null" : instruction.operand.ToString()));
					if (countReplace < 2 && instruction.opcode == OpCodes.Ldc_R4)
					{
						instruction.operand = 0.0f;
						countReplace++;
						Log.Message(string.Concat("Changed to: ", instruction.opcode.ToString(), " :: ", instruction.operand == null ? "null" : instruction.operand.ToString()));
					}
					return new List<CodeInstruction> { instruction };
				}
			}
			
			[HarmonyProcessorFactory]
			static HarmonyProcessor Disable_RNG_Death_Patch(MethodBase original)
			{
				var processor = new HarmonyProcessor();
				processor.Add(new RNGReplacer());
				return processor;
			}
		}
	}
}
*/

namespace CombatExtended.Harmony
{
	//Target Verse.HediffComp_TendDuration.CompTended
	//Specifically the line: this.tendQuality = Mathf.Clamp01(quality + Rand.Range(-0.25f, 0.25f));
	//Specifically the -0.25f, 0.25f
	
	[StaticConstructorOnStartup]
	static class Main
	{
		static Main()
		{
			var harmony = HarmonyInstance.Create("CombatExtended.Harmony.Verse.HediffComp_TendDuration.CompTended");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}
		
		[HarmonyPatch(typeof(HediffComp_TendDuration))]
		[HarmonyPatch("CompTended")]
		static class Tighten_RNG_Range
		{
			public class RNG_Range_Replacer : CodeProcessor
			{
				int countReplace = 0;
				
				public override List<CodeInstruction> Process(CodeInstruction instruction)
				{
					if (instruction == null) return null;
					//Log.Message(string.Concat("Instruction: ", instruction.opcode.ToString(), " :: ", instruction.operand == null ? "null" : instruction.operand.ToString()));
					if (countReplace < 2 && instruction.opcode == OpCodes.Ldc_R4)
					{
						instruction.operand = countReplace == 0 ? -0.15f : 0.15f;
						countReplace++;
						//Log.Message(string.Concat("Changed to: ", instruction.opcode.ToString(), " :: ", instruction.operand == null ? "null" : instruction.operand.ToString()));
					}
					return new List<CodeInstruction> { instruction };
				}
			}
			[HarmonyProcessorFactory]
			static HarmonyProcessor RNG_Range_Replacer_Patch(MethodBase original)
			{
				var processor = new HarmonyProcessor();
				processor.Add(new RNG_Range_Replacer());
				return processor;
			}
		}
	}
}
