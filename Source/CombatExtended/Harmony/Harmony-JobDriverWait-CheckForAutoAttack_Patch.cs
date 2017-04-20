using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using Harmony.ILCopying;
using Verse;
using Verse.AI;

/*
 * Targetting the Verse.AI.JobDriver_Wait.CheckForAutoAttack()
 * Target Line:
 *  Thing thing = AttackTargetFinder.BestShootTargetFromCurrentPosition(this.pawn, null, verb.verbProps.range, verb.verbProps.minRange, targetScanFlag);
 *  
 * Basically modify that line to read something like:
 *  Thing thing = AttackTargetFinder.BestShootTargetFromCurrentPosition(this.pawn, GetValidTargetPredicate(verb), verb.verbProps.range, verb.verbProps.minRange, targetScanFlag);
 * 
 * Overall does a couple of things.  First it locates the local variable with the verb used to attack with and second it locates the null argument in the above method call
 * for Predicate and replaces that with an arg stack load of the verb and a call to create a predicate.  That call removes the verb from the call stack and replaces it
 * with a predicate (or a null).
 * 
 * A couple of helper functions to turn a bunch of ifs into a single call since IL can use one of 6 instructions for local variable load/save.
 */

namespace CombatExtended.Harmony
{
    [HarmonyPatch(typeof(JobDriver_Wait))]
    [HarmonyPatch("CheckForAutoAttack")]
    static class Harmony_JobDriverWait_CheckForAutoAttack_Patch
    {
        static readonly string logPrefix = Assembly.GetExecutingAssembly().GetName().Name + " :: " + typeof(Harmony_JobDriverWait_CheckForAutoAttack_Patch).Name + " :: ";
        static DynamicMethod Patched_ClosestThingTarget_Global = null;

        /// <summary>
        /// Transpiler runs through the IL code of the method CheckForAutoAttack and makes some tweaks to a call so as to avoid having the pawn attack a target it can't hit.
        /// </summary>
        /// <param name="instructions">IEnumerable of CodeInstruction, required by Harmony, the IL code Harmony fetched/uses.</param>
        /// <returns>IEnumerable of CodeInstruction containing the changes to the method's IL code.</returns>
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int verbLocalIndex = -1;
            int indexKeyCall = 0;

            // turn instructions into a list so we can walk through it variably (instead of forward only).
            List<CodeInstruction> code = instructions.ToList();

            // walk forward to find some key information.
            for (int i = 0; i < code.Count(); i++)
            {
                // look for the verb instantiation/storage.
                {
                    MethodBase method = null;
                    if (code[i].opcode == OpCodes.Callvirt && (method = code[i].operand as MethodBase) != null && method.DeclaringType == typeof(Pawn) && method.Name == "TryGetAttackVerb"
                        && code.Count() >= i + 1)
                        verbLocalIndex = OpcodeStoreIndex(code[i + 1]);
                }

                // see if we've found the instruction index of the key call.
                if (code[i].opcode == OpCodes.Call && (code[i].operand as MethodInfo) != null)
                {
                   MethodInfo method = code[i].operand as MethodInfo;
                    if (method.DeclaringType == typeof(AttackTargetFinder) && method.Name == "BestShootTargetFromCurrentPosition")
                    {
                        indexKeyCall = i;
                        break;
                    }
                }
            }

            Log.Message(string.Concat("verbLocalIndex ", verbLocalIndex));
            // walk backwards from the key call to locate the null load and replace it with our call to drop in our predicate into the arg stack.
            for (int i = indexKeyCall; i >= 0; i--)
            {
                if (code[i].opcode == OpCodes.Ldnull)
                {
                    CodeInstruction tmp = MakeLocalLoadInstruction(verbLocalIndex);
                    Log.Message(string.Concat("Got hit, replacing instruction with: ", tmp.opcode, " type: ", (tmp.operand == null ? "null" : tmp.operand.GetType().ToString() + " = " + tmp.operand)));
                    code[i++] = MakeLocalLoadInstruction(verbLocalIndex);
                    code.Insert(i, new CodeInstruction(OpCodes.Call, typeof(Harmony_JobDriverWait_CheckForAutoAttack_Patch).GetMethod("GetValidTargetPredicate", AccessTools.all)));
                    break;
                }
            }

            return code;
        }

        /// <summary>
        /// Return a CodeInstruction object with the correct opcode to fetch a local variable at a specific index.
        /// </summary>
        /// <param name="index">int value specifying the local variable index to fetch.</param>
        /// <returns>CodeInstruction object with the correct opcode to fetch a local variable into the evaluation stack.</returns>
        static CodeInstruction MakeLocalLoadInstruction(int index)
        {
            // argument check...
            if (index < 0 || index > UInt16.MaxValue)
                throw new ArgumentException("Index must be greater than 0 and less than " + uint.MaxValue.ToString() + ".");

            // the first 4 are easy...
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

            // proper type info for the other items.
            if (index > Byte.MaxValue) return new CodeInstruction(OpCodes.Ldloc, index);
            return new CodeInstruction(OpCodes.Ldloc_S, index);
        }

        /// <summary>
        /// Return the index of a local variable (based on storage opcode).
        /// </summary>
        /// <param name="instruction">CodeInstruction object from Harmony</param>
        /// <returns>int index of the local variable the instruction refers to. -1 if the opcode wasn't a storage opcode.</returns>
        static int OpcodeStoreIndex(CodeInstruction instruction)
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
        /// Returns a predicate for valid targets if the verb is a Verb_LaunchProjectileCE or descendent.
        /// </summary>
        /// <param name="verb">Verb that is to be checked for type and used for valid target checking</param>
        /// <returns>Predicate of type Thing which indicates if that thing is a valid target for the pawn.</returns>
        static Predicate<Thing> GetValidTargetPredicate(Verb verb)
        {
            Predicate<Thing> predicate = null;
            if ((verb as Verb_LaunchProjectileCE) != null)
            {
                Verb_LaunchProjectileCE verbCE = verb as Verb_LaunchProjectileCE;
                predicate = t => verbCE.CanHitTargetFrom(verb.caster.Position, new LocalTargetInfo(t));
            }

            return predicate;
        }
    }
}