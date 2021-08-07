using System.Reflection;
using HarmonyLib;
using Verse;
using System;
using System.Reflection.Emit;
using System.Linq;
using System.Collections.Generic;

/* Note to those unfamiliar with Reflection/Harmony (like me, ProfoundDarkness), operands have some specific types and it's useful to know these to make good patches (Transpiler).
 * Below I'm noting the operators and what type of operand I've observed.
 * 
 * local variable operands who's index is < 4 (so _0, _1, _2, _3) seem to have an operand of null.
 * 
 * local variable operands (ex Ldloc_s, Stloc_s) == LocalVariableInfo
 * field operands (ex ldfld) == FieldInfo
 * method operands (ex call, callvirt) == MethodInfo
 * argument operands (ex Ldarga_S) == byte? (that's not asking a question but the actual type)
 * For branching, if the operand is a branch the label will be a Label? (Label isn't nullable but since it's in an object it must be nullable).
 * -Labels tend to look the same, use <instance_label>.GetHashCode() to determine WHICH label it is...
 * 
 * A useful IL instruction reference (for me): https://stackoverflow.com/questions/7212255/cecil-instruction-operand-types-corresponding-to-instruction-opcode-code-value#7215711
 * 
 */

namespace CombatExtended.HarmonyCE
{
    public static class HarmonyBase
    {
        private static Harmony harmony = null;

        /// <summary>
        /// Fetch CombatExtended's instance of Harmony.
        /// </summary>
        /// <remarks>One should only have a single instance of Harmony per Assembly.</remarks>
        static internal Harmony instance
        {
            get
            {
                if (harmony == null)
                    harmony = new Harmony("CombatExtended.HarmonyCE");
                return harmony;
            }
        }

        public static void InitPatches()
        {
            // Remove the remark on the following to debug all auto patches.
            // Harmony.DEBUG = true;
            instance.PatchAll(Assembly.GetExecutingAssembly());
            // Keep the following remarked to also debug manual patches.
            //HarmonyInstance.DEBUG = false;

            // Manual patches
            PatchThingOwner();
            PatchHediffWithComps(instance);
            Harmony_GenRadial_RadialPatternCount.Patch();
            PawnColumnWorkers_Resize.Patch();
            PawnColumnWorkers_SwapButtons.Patch();
        }

        #region Patch helper methods

        private static void PatchThingOwner()
        {
            // Need to patch ThingOwner<T> manually for all child classes of Thing
            var baseThingOwnerType = typeof(ThingOwner);
            //Post add notification patches
            var postfixNotifyAdded = typeof(Harmony_ThingOwner_NotifyAdded_Patch).GetMethod("Postfix");
            instance.Patch(baseThingOwnerType.GetMethod("NotifyAdded", BindingFlags.Instance | BindingFlags.NonPublic), null, new HarmonyMethod(postfixNotifyAdded));
            var postfixNotifyAddedAndMergedWith = typeof(Harmony_ThingOwner_NotifyAddedAndMergedWith_Patch).GetMethod("Postfix");
            instance.Patch(baseThingOwnerType.GetMethod("NotifyAddedAndMergedWith", BindingFlags.Instance | BindingFlags.NonPublic), null, new HarmonyMethod(postfixNotifyAddedAndMergedWith));
            //Post take notification patch
            var postfixTake = typeof(Harmony_ThingOwner_Take_Patch).GetMethod("Postfix");
            instance.Patch(baseThingOwnerType.GetMethod("Take", new Type[] { typeof(Thing), typeof(int) }), null, new HarmonyMethod(postfixTake));
            //Post remove notification patch
            var postfixRemove = typeof(Harmony_ThingOwner_NotifyRemoved_Patch).GetMethod("Postfix");
            instance.Patch(baseThingOwnerType.GetMethod("NotifyRemoved", BindingFlags.Instance | BindingFlags.NonPublic), null, new HarmonyMethod(postfixRemove));
            /*
            var postfixTryAdd = typeof(Harmony_ThingOwner_TryAdd_Patch).GetMethod("Postfix");
            var postfixTake = typeof(Harmony_ThingOwner_Take_Patch).GetMethod("Postfix");
            var postfixRemove = typeof(Harmony_ThingOwner_Remove_Patch).GetMethod("Postfix");

            var baseType = typeof(Thing);
            var types = baseType.AllSubclassesNonAbstract().AddItem(baseType);
            foreach (Type current in types)
            {
                var type = typeof(ThingOwner<>).MakeGenericType(current);
                instance.Patch(type.GetMethod("TryAdd", new Type[] { typeof(Thing), typeof(bool) }), null, new HarmonyMethod(postfixTryAdd));
                instance.Patch(type.GetMethod("Take", new Type[] { typeof(Thing), typeof(int) }), null, new HarmonyMethod(postfixTake));
                instance.Patch(type.GetMethod("Remove", new Type[] { typeof(Thing) }), null, new HarmonyMethod(postfixRemove));
            }*/
        }

        private static void PatchHediffWithComps(Harmony harmonyInstance)
        {
            var postfixBleedRate = typeof(Harmony_HediffWithComps_BleedRate_Patch).GetMethod("Postfix");
            var baseType = typeof(HediffWithComps);
            var types = baseType.AllSubclassesNonAbstract();
            foreach (Type cur in types)
            {
                var getMethod = cur.GetProperty("BleedRate").GetGetMethod();
                if (getMethod.IsVirtual && (getMethod.DeclaringType.Equals(cur)))
                {
                    harmonyInstance.Patch(getMethod, null, new HarmonyMethod(postfixBleedRate));
                }
            }
        }

#endregion

#region Utility_Methods

// Remarked the following block since time is a factor, played with it yesterday but it will probably eat too much time to finish and is probably a better fit for 
// a Harmony PR.
/*
/// <summary>
/// Returns a bool indicating if the types are compatible (castable).  Optional bool does implicit specific check.  That is that one can cast from into to.
/// </summary>
/// <param name="from">Type of object that moving from.</param>
/// <param name="to">Type of object that moving to.</param>
/// <param name="implicitly">bool indicating if the from->to cast is limited to implicit casting.</param>
/// <returns>bool true indicates the cast can happen, false not.</returns>
/// <remarks>based on https://stackoverflow.com/questions/2119441/check-if-types-are-castable-subclasses </remarks>
public static bool IsCastableTo(this Type from, Type to, bool implicitly = false)
{
    return to.IsAssignableFrom(from) || from.HasCastDefined(to, implicitly);
}
private static bool HasCastDefined(this Type from, Type to, bool implicitly)
{
    if ((from.IsPrimitive || from.IsEnum) && (to.IsPrimitive || to.IsEnum))
    {
        if (!implicitly)
            return from == to || (from != typeof(Boolean) && to != typeof(Boolean));

        Type[][] typeHierarchy = {
    new Type[] { typeof(Byte),  typeof(SByte), typeof(Char) },
    new Type[] { typeof(Int16), typeof(UInt16) },
    new Type[] { typeof(Int32), typeof(UInt32) },
    new Type[] { typeof(Int64), typeof(UInt64) },
    new Type[] { typeof(Single) },
    new Type[] { typeof(Double) }
};
        IEnumerable<Type> lowerTypes = Enumerable.Empty<Type>();
        foreach (Type[] types in typeHierarchy)
        {
            if (types.Any(t => t == to))
                return lowerTypes.Any(t => t == from);
            lowerTypes = lowerTypes.Concat(types);
        }

        return false;   // IntPtr, UIntPtr, Enum, Boolean
    }
    return IsCastDefined(to, m => m.GetParameters()[0].ParameterType, _ => from, implicitly, false)
        || IsCastDefined(from, _ => to, m => m.ReturnType, implicitly, true);
}
static bool IsCastDefined(Type type, Func<MethodInfo, Type> baseType,
                        Func<MethodInfo, Type> derivedType, bool implicitly, bool lookInBase)
{
    var bindinFlags = BindingFlags.Public | BindingFlags.Static
                    | (lookInBase ? BindingFlags.FlattenHierarchy : BindingFlags.DeclaredOnly);
    return type.GetMethods(bindinFlags).Any(
        m => (m.Name == "op_Implicit" || (!implicitly && m.Name == "op_Explicit"))
            && baseType(m).IsAssignableFrom(derivedType(m)));
}


private static IEnumerable<CodeInstruction> doSwapCall (IEnumerable<CodeInstruction> instructions, ILGenerator il, Type[] tArgs, Type[] fArgs, int tIndex = 0)
{
    bool skipPatch = false;
    List<CodeInstruction> preCall = new List<CodeInstruction>();

    // Further error checking, make sure that each set of argument in 'from' and 'to' are compatible, if not then don't patch.
    for (int i = 0; i < fArgs.Length; i++)
    {
        if (!IsCastableTo(fArgs[i], tArgs[tIndex], true))
        {
            Log.Error(string.Concat("doSwapCall :: Invalid argument: 'from' Type (", fArgs[i], ") is not implicitly castable 'to' Type (", tArgs[tIndex], "). Patching skipped."));
            skipPatch = true;
            break;
        }
        tIndex++;
    }

    // identify the remaining args in tArgs so that we can insert appropriate instructions to pick them up before the call instruction.
    // there is a chance we can fail at this point...
    if (!skipPatch)
    {
        List<LocalBuilder> locals = Traverse.Create(il).Field("locals").GetValue<LocalBuilder[]>().ToList();
        MethodBase from;

        for (int i = tIndex; i < tArgs.Length; i++)
        {

        }
    }
}

internal static IEnumerable<CodeInstruction> SwapCallvirt (IEnumerable<CodeInstruction> instructions, ILGenerator il, MethodBase from, MethodBase to)
{
    int tIndex = 0;
    Type[] tArgs = to.GetGenericArguments();
    Type[] fArgs = from.GetGenericArguments();

    // first error check, 'to' needs to contain the calling type (from) as an argument.
    if (!IsCastableTo(from.DeclaringType, tArgs[tIndex], true))
    {
        Log.Error(string.Concat("SwapCallvirt :: Invalid argument: Initial Type (", tArgs[tIndex], ") is not implicitly castable from Type (", from.DeclaringType, "). Patching skipped."));
        return instructions;
    }

    // pass further execution onto the main workhorse.
    return doSwapCall(instructions, il, tArgs, fArgs, tIndex + 1);
}

internal static IEnumerable<CodeInstruction> SwapCall (IEnumerable<CodeInstruction> instructions, ILGenerator il, MethodBase from, MethodBase to)
{
    return doSwapCall(instructions, il, to.GetGenericArguments(), from.GetGenericArguments());
}
*/

internal static LocalBuilder[] GetLocals(ILGenerator il)
        {
            return Traverse.Create(il).Field("locals").GetValue<LocalBuilder[]>();
        }

        /// <summary>
        /// branchOps is used by isBranch utility method.
        /// </summary>
        private static readonly OpCode[] branchOps = {
            OpCodes.Br, OpCodes.Br_S, OpCodes.Brfalse, OpCodes.Brfalse_S, OpCodes.Brtrue, OpCodes.Brtrue_S, // basic branches
            OpCodes.Bge, OpCodes.Bge_S, OpCodes.Bge_Un, OpCodes.Bge_Un_S, OpCodes.Bgt, OpCodes.Bgt_S, OpCodes.Bgt_Un, OpCodes.Bgt_Un_S, // Branch Greater
            OpCodes.Ble, OpCodes.Ble_S, OpCodes.Ble_Un, OpCodes.Ble_Un_S, OpCodes.Blt, OpCodes.Blt_S, OpCodes.Blt_Un, OpCodes.Blt_Un_S, // Branch Less
            OpCodes.Beq, OpCodes.Beq_S, OpCodes.Bne_Un, OpCodes.Bne_Un_S // Branch Equality
        }; 

        /// <summary>
        /// Simple check to see if the instruction is a branching instruction (and if so the operand should be a label)
        /// </summary>
        /// <param name="instruction">CodeInstruction provided by Harmony.</param>
        /// <returns>bool, true means it is a branching instruction, false is it's not.</returns>
        internal static bool isBranch(CodeInstruction instruction)
        {
            if (branchOps.Contains(instruction.opcode))
                return true;
            return false;
        }

        /// <summary>
        /// Utility function to convert a nullable bool (bool?) into a bool (primitive).
        /// </summary>
        /// <param name="input">bool? (nullable bool)</param>
        /// <returns>bool</returns>
        internal static bool doCast(bool? input)
        {
            if (!input.HasValue)
                return false;
            return (bool)input;
        }

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
