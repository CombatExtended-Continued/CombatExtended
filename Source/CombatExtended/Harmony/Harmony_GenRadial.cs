using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace CombatExtended.HarmonyCE
{
	/* Targetting Verse.GenRadial.RadialPatternCount constant.
	 * Specifically this looks for all methods which use that constant and patches them (since the constant is compiled in).
	 * Also targets the -60 and 60 in Verse.GenRadial.SetupRadialPattern as it turns out those are important too.
	 * 
	 * Should cause RimWorld to take a tad longer to startup but shouldn't affect game speed while playing.
	 */ 
	
	// NOTE: When using a complex patch (where Patch()) must be used, do not include any Harmony class Attributes.  Method Attributes are optional as the Patch() handles what type of patch something is.
	internal static class Harmony_GenRadial_RadialPatternCount
	{
		// used for debug outputs (probably not used much).
		static readonly string logPrefix = "Combat Extended :: " + typeof(Harmony_GenRadial_RadialPatternCount).Name + " :: ";
		
		// this replaces -60 and 60 in SetupRadialPattern()
		const SByte newRadialRange = SByte.MaxValue; // set to max value as I was creeping up on it to make things look right.  Though no crashes the circle was squished on cardinal directions.
		
		const double keepPatternRange = 10000f / 14400f;
		
		// this replaces the constant RadialPaternCount used in the class methods/constructor of GenRadial.
		static readonly Int32 newRadialPatternCount = Convert.ToInt32(Math.Pow(newRadialRange * 2, 2) * keepPatternRange); //may be any valid int <= Math.Pow(newRange*2, 2)
        // on further thought I think the limit should be about 68 percent of the maximum available by the loop in order to avoid squishing.

        static Int32 defaultValue;

		// This is a complex patch so PatchAll() can't cover it.
		internal static void Patch()
		{
			IEnumerable<MethodInfo> methods = typeof(GenRadial).GetMethods(AccessTools.all).Where(m => m.DeclaringType == typeof(GenRadial));
			// (ProfoundDarkness) I tried to get the individual default constructor via GetConstructor() but didn't know enough of what I was doing.
			ConstructorInfo constructor = typeof(GenRadial).GetConstructors(AccessTools.all).FirstOrDefault();
			MethodInfo redoMethod = null;

            defaultValue = GenRadial.RadialPatternCount;

			// patch the constructor.
			HarmonyBase.instance.Patch(constructor, null, null, new HarmonyMethod(typeof(Harmony_GenRadial_RadialPatternCount), "Transpiler_RadialPatternCount"));
			
			// patch all the methods.
			foreach(MethodInfo method in methods)
			{
				HarmonyBase.instance.Patch(method, null, null, new HarmonyMethod(typeof(Harmony_GenRadial_RadialPatternCount), "Transpiler_RadialPatternCount"));
				if (method.Name == "SetupRadialPattern")
				{
					// this method is useful to remember and we need to double patch it.
					redoMethod = method;
					HarmonyBase.instance.Patch(method, null, null, new HarmonyMethod(typeof(Harmony_GenRadial_RadialPatternCount), "Transpiler_Range"));
				}
			}
			
			// Need to re-run constructor code since the constructor is static and I don't know how to do that via reflection.
			// (ProfoundDarkness) Tried MANY things and none worked.  I could make the change to the binary directly but my lack of C# and Reflection knowledge gets in the way again...  
			//  The following 3 lines are the key bits that need to be re-run by the constructor so put them here.
			Traverse.Create(typeof(GenRadial)).Field("RadialPattern").SetValue(new IntVec3[newRadialPatternCount]);
			Traverse.Create(typeof(GenRadial)).Field("RadialPatternRadii").SetValue(new float[newRadialPatternCount]);
			redoMethod.Invoke(null, null);
			
			Log.Message(string.Concat(logPrefix, "Info: Post GenRadial patch maximum radius: ", GenRadial.MaxRadialPatternRadius));
		}
		
		static IEnumerable<CodeInstruction> Transpiler_RadialPatternCount(IEnumerable<CodeInstruction> instructions)
		{
			foreach (CodeInstruction instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldc_I4 && (Int32)instruction.operand == defaultValue)
					instruction.operand = newRadialPatternCount;
			}
			return instructions;
		}
		
		// This transpiler converts the -60 and 60 values in the first couple of loops.  May need to change opcodes to int if SByte.MaxValue is insufficient.
		static IEnumerable<CodeInstruction> Transpiler_Range(IEnumerable<CodeInstruction> instructions)
		{
			const sbyte defaultVal = 60;
			int foundNeg = 0;
			int foundPos = 0;
			
			foreach (CodeInstruction instruction in instructions)
			{
				if (foundPos >= 2)
					continue;
				
				if (foundNeg < 2 && instruction.opcode == OpCodes.Ldc_I4_S && (SByte)instruction.operand == -defaultVal)
				{
					instruction.operand = (sbyte)-newRadialRange;
					foundNeg++;
				}
				
				if (foundNeg >= 2 && foundPos < 2 && instruction.opcode == OpCodes.Ldc_I4_S && (SByte)instruction.operand == defaultVal)
				{
					instruction.operand = newRadialRange;
					foundPos++;
				}
			}
			
			return instructions;
		}
	}
}