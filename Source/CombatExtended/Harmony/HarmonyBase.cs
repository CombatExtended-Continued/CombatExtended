using System.Reflection;
using Harmony;
using Verse;
using System;
using System.Reflection.Emit;

/* Note to those unfamiliar with Reflection/Harmony (like me, ProfoundDarkness), operands have some specific types and it's useful to know these to make good patches (Transpiler).
 * Below I'm noting the operators and what type of operand I've observed.
 * 
 * local variable operands who's index is < 4 (so _0, _1, _2, _3) seem to have an operand of null.
 * 
 * local variable operands (ex Ldloc_s, Stloc_s) == LocalVariableInfo
 * field operands (ex ldfld) == FieldInfo
 * method operands (ex call, callvirt) == MethodInfo
 */

namespace CombatExtended.Harmony
{
	[StaticConstructorOnStartup]
	static class HarmonyBase
	{
		private static HarmonyInstance harmony = null;
		
		static HarmonyBase()
		{
			// Unremark the following when developing new Harmony patches (Especially Transpilers).  The file "harmony.log.txt" on your desktop and is always appended.  Will cause ALL patches to be debugged.
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

        #region Utility_Methods
        /// <summary>
        /// Return a CodeInstruction object with the correct opcode to fetch a local variable at a specific index.
        /// </summary>
        /// <param name="index">int value specifying the local variable index to fetch.</param>
        /// <param name="info">LocalVariableInfo optional if you've stored the metadata from a load/save instruction previously.</param>
        /// <returns>CodeInstruction object with the correct opcode to fetch a local variable into the evaluation stack.</returns>
        internal static CodeInstruction MakeLocalLoadInstruction(int index, LocalVariableInfo info = null)
        {
            // argument check...
            if (index < 0 || index > UInt16.MaxValue)
                throw new ArgumentException("Index must be greater than 0 and less than " + uint.MaxValue.ToString() + ".");

            // the first 4 are easy...  (ProfoundDarkness)NOTE: I've only ever observed these with null operands.
            switch (index)
            {
                case 0:
                    return new CodeInstruction(OpCodes.Ldloc_0);
                case 1:
                    return new CodeInstruction(OpCodes.Ldloc_1);
                case 2:
                    return new CodeInstruction(OpCodes.Ldloc_2);
                case 3:
                    return new CodeInstruction(OpCodes.Ldloc_3);
            }

            object objIndex;
            // proper type info for the other items.
            if (info != null && info.LocalIndex == index)
                objIndex = info;
            else
                objIndex = index;

            if (index > Byte.MaxValue) return new CodeInstruction(OpCodes.Ldloc, objIndex);
            return new CodeInstruction(OpCodes.Ldloc_S, objIndex);
        }

        /// <summary>
        /// Return the index of a local variable (based on storage opcode).
        /// </summary>
        /// <param name="instruction">CodeInstruction object from Harmony</param>
        /// <returns>int index of the local variable the instruction refers to. -1 if the opcode wasn't a recognized storage opcode.</returns>
        internal static int OpcodeStoreIndex(CodeInstruction instruction)
        {
            if (instruction.opcode == OpCodes.Stloc_0) return 0;
            if (instruction.opcode == OpCodes.Stloc_1) return 1;
            if (instruction.opcode == OpCodes.Stloc_2) return 2;
            if (instruction.opcode == OpCodes.Stloc_3) return 3;
            if (instruction.opcode == OpCodes.Stloc_S) // UInt8
                return (instruction.operand as LocalVariableInfo).LocalIndex;
            if (instruction.opcode == OpCodes.Stloc) // UInt16
                return (instruction.operand as LocalVariableInfo).LocalIndex;
            return -1; // error, unrecognized opcode so can check this if we DIDN'T get an apt opcode.
        }

        /// <summary>
        /// REturn the index of a local variable (based on load opcode).
        /// </summary>
        /// <param name="instruction">CodeInstruction object from Harmony</param>
        /// <returns>int index of the local variable the instruction refers to.  -1 if the opcode wasn't a recognized load opcode.</returns>
        internal static int OpcodeLoadIndex(CodeInstruction instruction)
        {
            if (instruction.opcode == OpCodes.Ldloc_0) return 0;
            if (instruction.opcode == OpCodes.Ldloc_1) return 1;
            if (instruction.opcode == OpCodes.Ldloc_2) return 2;
            if (instruction.opcode == OpCodes.Ldloc_3) return 3;
            if (instruction.opcode == OpCodes.Ldloc_S) // UInt8
                return (instruction.operand as LocalVariableInfo).LocalIndex;
            if (instruction.opcode == OpCodes.Ldloc) // UInt16
                return (instruction.operand as LocalVariableInfo).LocalIndex;
            return -1;
        }
        #endregion Utility_Methods
    }
}
