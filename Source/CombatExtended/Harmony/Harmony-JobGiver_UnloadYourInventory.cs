using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.AI;

/* Concept: Change the line:
 *  if (!pawn.inventory.UnloadEverything)
 * to:
 *  if (!pawn.inventory.UnloadEverything && !pawn.HasAnythingForDrop())
 * In the IL the logic is a tad different...
 *  L_0006: callvirt Boolean get_UnloadEverything()
 *  L_000b: brtrue Label #2
 *  L_0010: ldnull
 *  L_0011: br Label #0
 * 
 */

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(JobGiver_UnloadYourInventory), "TryGiveJob", new Type[] { typeof(Pawn) } )]
    static class Harmony_JobGiver_UnloadYourInventory
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase source, ILGenerator il)
        {
            // another new thing, find the desired parameter instead of assuming it will be the same.
            ParameterInfo[] args = source.GetParameters();
            int argIndex = -1;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].ParameterType.Equals(typeof(Pawn)))
                {
                    argIndex = i + 1; // parrameters are 0 based because of instance.
                    break;
                }
            }

            Label branchFalse = il.DefineLabel(); // since the logic is gettin changed more than I thought, need a new label.


            int patchPhase = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                // Looking for the ldnull instruction to add a label to it.
                if (patchPhase == 1 && instruction.opcode.Equals(OpCodes.Ldnull))
                {
                    instruction.labels.Add(branchFalse);

                    patchPhase = 2;
                }

                // The first branch we find is the one to remember.  This is also the insertion point...
                if (patchPhase == 0 && instruction.opcode.Equals(OpCodes.Brtrue_S))
                {

                    // If the inventory isn't set to unload mode, short circuit and jump to return null path...
                    yield return new CodeInstruction(OpCodes.Brfalse, branchFalse);

                    // load the arg which is the pawn.
                    yield return new CodeInstruction(OpCodes.Ldarg, argIndex);
                    // load call to see if the pawn has anything they can drop according to loadout/holdtracker.
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Utility_HoldTracker), "HasAnythingForDrop"));

                    // leave the current instruction, branch true, alone...

                    patchPhase = 1;
                }

                yield return instruction;

            }

            if (patchPhase < 2)
                Log.Warning("CombatExtended :: Harmony-JobGiver_UnloadYourInventory patch failed to complete all its steps");
        }
    }
}
