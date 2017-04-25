using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using Verse;
using RimWorld;
using Verse.AI;

// (ProfoundDarkness) Disabled this as I forgot to check and see if there was a detour again... and there was.  Kept around as reference for eventual Harmony conversion of detours after A17.

namespace CombatExtended.Harmony
{
    /* Targeting RimWorld.WorkGiver_InteractAnimal.TakeFoodForAnimalInteractJob()
     * Specifically inserting a notification right before the return in the line:
     *  return new Job(JobDefOf.TakeInventory, thing)
     * which will notify HoldTracker to hold onto the item.
     * 
     * There isn't really an obvious code way to put what happens but something sortof like this:
     *  Job job = new Job(JobDefOf.TakeInventory, thing);
     *  pawn.Notify_HoldTracker(job);
     *  return job;
     *  
     * Except that no new local variable is being created.
     */

    [HarmonyPatch(typeof(WorkGiver_InteractAnimal))]
    [HarmonyPatch("TakeFoodForAnimalInteractJob")]
    [HarmonyPatch(new Type[] { typeof(Pawn), typeof(Pawn) })]
    static class Harmony_WorkGiver_InteractAnimal_TakeFoodforAnimalInteractJob
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Note: First argument is the Pawn that will get the job.

            bool foundTakeInventory = false;
            int jobLocalIndex = -1;
            int instructionIndex = 0;
            int lastLoadJobIndex = -1;
            LocalVariableInfo jobInfo = null;

            // First we look for stuff.
            foreach (CodeInstruction instruction in instructions)
            {
                // Locate the local variable that the job gets stored to.
                if (foundTakeInventory && jobLocalIndex < 0)
                    jobLocalIndex = HarmonyBase.OpcodeStoreIndex(instruction);

                if (instruction.opcode == OpCodes.Ldsfld && (instruction.operand as FieldInfo).FieldType == typeof(JobDef) && (instruction.operand as FieldInfo).Name == "TakeInventory")
                    foundTakeInventory = true;

                // Locate the last local load instruction coresponding to the local variable that the job was stored.  Insertion point is above this.
                if (jobLocalIndex >= 0 && HarmonyBase.OpcodeLoadIndex(instruction) == jobLocalIndex)
                {
                    lastLoadJobIndex = instructionIndex;
                    jobInfo = instruction.operand as LocalVariableInfo;  // capture the metadata about this instruction for use later.
                }
                instructionIndex++;
            }

            // now lets start altering code...
            int curInstructionIndex = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                if (lastLoadJobIndex == curInstructionIndex)
                {
                    // first argument we need is the pawn (1st argument in host method)...
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    // next argument is the job...
                    yield return HarmonyBase.MakeLocalLoadInstruction(jobLocalIndex, jobInfo);
                    // now the call to the notifier...
                    yield return new CodeInstruction(OpCodes.Call, typeof(Utility_HoldTracker).GetMethod("Notify_HoldTrackerJob", new Type[] { typeof(Pawn), typeof(Job) }));
                }

                yield return instruction;
                curInstructionIndex++;
            }
        }
    }
}