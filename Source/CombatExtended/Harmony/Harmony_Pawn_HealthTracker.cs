using System.Collections.Generic;
using Verse;
using HarmonyLib;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;

namespace CombatExtended.HarmonyCE
{
    /* Dev Notes:
     * The goal in this case is to remove the RNG death event.
     * Can be accomplished by setting some key values to 0 so that the random number can never be smaller.
     * Can also be accomplished by removing code/forcing a branch to skip the undesired code (depends on assembly output).
     * 
     * Looking at the IL, basically after the branch false following this.ShouldBeDowned() wipe out all code up to
     * 'this.forceIncap = false' line.
     * Use the label in the branch detected above to figure out if we went too far.  There is no code insertion, just removal.
     * 
     * // removing code is risky since there could be a labels issue...
     */

    [HarmonyPatch(typeof(Pawn_HealthTracker), "CheckForStateChange")]
    static class Harmony_Pawn_HealthTracker_CheckForStateChange
    {


        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int patchPhase = 0;
            List<CodeInstruction> previous = new List<CodeInstruction>();
            List<Label> removed = new List<Label>();

            foreach (CodeInstruction instruction in instructions)
            {
                // looking for "stfld System.Boolean forceIncap" to stop deleting instructions...
                if (patchPhase == 2 && instruction.opcode == OpCodes.Stfld && HarmonyBase.doCast((instruction.operand as FieldInfo).Name == "forceIncap"))
                {
                    // write out the previous 2 instructions...  Should be an ldarg and ldc but that's not tested.
                    yield return previous[previous.Count - 2]; //count is 1 base, need 0 base to access as an array.
                    yield return previous.Last();

                    patchPhase = 3;
                }

                // looking for 2 instructions in a row, "call Boolean ShouldBeDowned()" and "brfalse Label #(...)", when found stop yielding instructions for a bit.
                if (patchPhase == 0 && !previous.NullOrEmpty() && instruction.opcode == OpCodes.Brfalse && previous.Last().opcode == OpCodes.Call
                    && HarmonyBase.doCast((previous.Last().operand as MethodInfo)?.Name.Equals("ShouldBeDowned"))
                    && HarmonyBase.doCast((previous.Last().operand as MethodInfo)?.DeclaringType.Equals(typeof(Pawn_HealthTracker))))
                    patchPhase = 1;

                // Need to catch branches and labels to maintain branching state of the method.
                if (patchPhase == 2)
                {
                    if (HarmonyBase.isBranch(instruction) && (instruction.operand as Label?).HasValue)
                        removed.Add((instruction.operand as Label?).Value);
                    if (!instruction.labels.NullOrEmpty())
                        cleanLabels(instruction.labels, removed);
                }

                // remember previous instructions only as necessary.
                if (patchPhase < 3)
                    previous.Add(instruction);

                // clean up any lingering labels that were from branches in removed code.
                if (patchPhase == 3 && !removed.NullOrEmpty() && !instruction.labels.NullOrEmpty())
                    cleanLabels(instruction.labels, removed);

                // if we aren't in the patch phase for removing instructions, yield them.
                if (patchPhase != 2)
                    yield return instruction;

                // transition point, we want to yield the instruction above but then stop yielding them after this.
                if (patchPhase == 1)
                {
                    previous.Clear();
                    patchPhase = 2;
                }
            }
        }

        /// <summary>
        /// Searches the 'clean' list for any items in 'dirty'.  Any items shared in both are removed from both.
        /// </summary>
        /// <param name="clean">List of Label, the generally from the instruction to be cleaned up</param>
        /// <param name="dirty">List of Label, the list of labels which are considered dirty and should be removed.</param>
        /// <remarks>The reason for removing from both lists is that labels can only be defined once and only used once.</remarks>
        private static void cleanLabels(List<Label> clean, List<Label> dirty)
        {
            for (int i = 0; i < clean.Count; i++)
            {
                int j = -1;
                if ((j = dirty.FindIndex(l => l.GetHashCode() == clean[i].GetHashCode())) > 0)
                {
                    clean.RemoveAt(i);
                    dirty.RemoveAt(j);
                    i--;
                }
            }
        }
    }
}
