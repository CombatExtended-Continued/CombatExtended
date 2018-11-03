using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Harmony;
using Verse;
using RimWorld;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CombatExtended.Harmony
{
    /* Dev Notes:
     * on a high level need to make the following changes.
     * From:
     *  float single = 0.2f;
     * To:
     *  float single = 1f;
     * From:
     *  single = single * Mathf.Clamp01(1f - hediffCompTendDuration.tendQuality);
     * To: (Decided to make this a call rather than try and patch with specifics, easier to maintain/tweak)
     *  single = Mathf.Clamp01(chance / Mathf.Pow(hediffCompTendDuration.tendQuality + 0.75f, 2));
     * 
     * Plan to make the first change as part of the patch.
     * The second change will remove the original code (so remove single * Mathf.Clamp01 (etc...))
     *  and instead assign single the value from a function call for easier tweaking in the future.
     */

    [HarmonyPatch(typeof(HediffComp_GetsPermanent), "CompPostInjuryHeal")]
    static class HediffComp_GetsOld_CompPostInjuryHeal_Patch
    {
        static readonly string logPrefix = Assembly.GetExecutingAssembly().GetName().Name + " :: " + typeof(HediffComp_GetsOld_CompPostInjuryHeal_Patch).Name + " :: ";

        // Change this value to tweak the base value used by the original function.
        static Single baseChance = 1f;

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            object tendLocal = null;
            // trying something different, asking the ILGenerator about the local variables to find the one with the desired type... (got the idea from Harmony source)
            foreach (LocalBuilder local in Traverse.Create(il).Field("locals").GetValue<LocalBuilder[]>())
            {
                if (local.LocalType == typeof(HediffComp_TendDuration))
                {
                    tendLocal = local;
                }
            }

            MethodInfo newMath = AccessTools.Method(typeof(HediffComp_GetsOld_CompPostInjuryHeal_Patch), "ClampedValue");
            int chanceLocalIndex = -1;

            int patchPhase = 0;

            if (tendLocal != null)
            {
                foreach (CodeInstruction instruction in instructions)
                {
                    // look for the store opcode that indicate the insert point.
                    if (patchPhase == 5 && HarmonyBase.OpcodeStoreIndex(instruction) >= 0)
                    {
                        // basically after removing a whole heap of instructions (between a branch and a store which preceeds a labeled instruction) we insert our call.

                        // insert the load instruction for chance
                        yield return HarmonyBase.MakeLocalLoadInstruction(chanceLocalIndex);
                        // insert the (local) HediffComp_TendDuration.tendQuality instructions.
                        yield return new CodeInstruction(OpCodes.Ldloc, tendLocal);
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(HediffComp_TendDuration), "tendQuality"));
                        // insert the call
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HediffComp_GetsOld_CompPostInjuryHeal_Patch), "ClampedValue"));

                        // change the mode so the store instruction that we found is retained.
                        patchPhase = 6;
                    }

                    // find the branch false (or rather branch if null) instruction.
                    if (patchPhase == 3 && instruction.opcode == OpCodes.Brfalse)
                        patchPhase = 4;

                    // find the instruction that gets HediffComp_TendDuration so we know where we are in the code some.
                    if (patchPhase == 2 && instruction.opcode == OpCodes.Call && HarmonyBase.doCast((instruction.operand as MethodInfo)?.ReturnType.Equals(typeof(HediffComp_TendDuration)))
                        && HarmonyBase.doCast((instruction.operand as MethodInfo)?.Name.Equals("TryGetComp")))
                        patchPhase = 3;

                    // the next store event will be the local variable we want to save for later.
                    if (patchPhase == 1 && HarmonyBase.OpcodeStoreIndex(instruction) >= 0)
                    {
                        chanceLocalIndex = HarmonyBase.OpcodeStoreIndex(instruction);
                        patchPhase = 2;
                    }

                    // search for and change the 0.2f float value to 1, should only be found once since it's stored to a local variable.
                    if (patchPhase == 0 && instruction.opcode == OpCodes.Ldc_R4 && (instruction.operand as Single?) != null && (instruction.operand as Single?).Equals(0.2f))
                    {
                        instruction.operand = baseChance;
                        patchPhase = 1;
                    }

                    // in phase 5 we are removing a whole heap of instructions and replacing them all with 1 call, so in that phase don't write any instructions out.
                    if (patchPhase != 5)
                        yield return instruction;
                    // phase 4 is a special transitional case, we want to stop storing instructions AFTER writing the instruction that caused us to switch modes.
                    if (patchPhase == 4)
                    {
                        patchPhase = 5;
                    }
                }
            }
            else
            {
                Log.Error(string.Concat(logPrefix, "Unable to locate expected local variable, patching skipped."));
                foreach (CodeInstruction instruction in instructions)
                    yield return instruction;
            }
        }

        /// <summary>
        /// Does the calculation related to scarring chance (I think).
        /// </summary>
        /// <param name="chance">float base chance as set by the caller.</param>
        /// <param name="tendQuality">float value from current instance of HediffComp_TendDuration.tendQuality property.</param>
        /// <returns>float</returns>
        /// <remarks>You MUST camp the value before return, ie return Mathf.Clamp01(calculation)</remarks>
        static float ClampedValue(float chance, float tendQuality)
        {
            return Mathf.Clamp01(chance / Mathf.Pow(tendQuality + 0.75f, 2));
        }
    }
}