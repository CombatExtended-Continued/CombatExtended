using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.AI;

namespace CombatExtended.Harmony
{
    /* This class handles patching of the Core PawnColumnWorkers that are used on the assign tab which are similar to the PawnColumnWorker_Loadout.
     * This patch reads the variables from PawnColumnWorker_Loadout and sets Core to use the same values so all 3 workers use the same size information.
     * This is relatively straight forward in that it's just replacing some constants.
     */
    static class PawnColumnWorkers_Resize
    {
        static readonly string logPrefix = Assembly.GetExecutingAssembly().GetName().Name + " :: " + typeof(PawnColumnWorkers_Resize).Name + " :: ";

        static readonly float orgMinWidth = 194f;
        static readonly float orgOptimalWidth = 354f;

        /// <summary>
        /// Handles the patch work as this is more efficiently handled as a complex patch instead of automatically by Harmony.
        /// </summary>
        public static void Patch()
        {
            Type[] targetTypes = new Type[] { typeof(PawnColumnWorker_Outfit), typeof(PawnColumnWorker_DrugPolicy) };
            string[] targetNames = new string[] { "GetMinWidth", "GetOptimalWidth" };
            HarmonyMethod[] transpilers = new HarmonyMethod[] { new HarmonyMethod(typeof(PawnColumnWorkers_Resize), "MinWidth"),
                                                                new HarmonyMethod(typeof(PawnColumnWorkers_Resize), "OptWidth")};

            for (int i = 0; i < targetTypes.Length; i++)
            {
                for (int j = 0; j < targetNames.Length; j++)
                {
                    MethodBase method = targetTypes[i].GetMethod(targetNames[j], AccessTools.all);
                    HarmonyBase.instance.Patch(method, null, null, (HarmonyMethod) transpilers[j]);
                }
            }
        }

        /// <summary>
        /// Transpiler for GetMinWidth method in Core.  Replaces constant values in the original method with constants from CE.
        /// </summary>
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> MinWidth(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && (instruction.operand as float?).HasValue && (instruction.operand as float?).Value.Equals(orgMinWidth))
                {
                    instruction.operand = PawnColumnWorker_Loadout._MinWidth;
                }
                yield return instruction;
            }
        }

        /// <summary>
        /// Transpiler for GetOptimalWidth method in Core.  Replaces constant values in the original method with constants from CE.
        /// </summary>
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> OptWidth(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && (instruction.operand as float?).HasValue && (instruction.operand as float?).Value.Equals(orgOptimalWidth))
                {
                    instruction.operand = PawnColumnWorker_Loadout._OptimalWidth;
                }
                yield return instruction;
            }
        }
    }

    /* This class handles patching of two of the Core PawnColumnWorkers to use the same buttons as PawnColumnWorker_Loadout.
     * This is accomplished by inserting an additional local variable that handles the y offset from top as the icons are smaller than text buttons,
     * and modifying the size (Rect) of the stock buttons to fit icon size.
     * It then modifies the last Rect objects (1 or 2) to fit icon size instead of text button size as well as using icons instead of text buttons.
     * Finally it appends the last Rect object (edit button) with a tooltip since the original button didn't have one.
     * 
     * This is a bit of a complicated one in that it's a single patcher for both methods and one of the PawnColumnWorkers needs 2 iterations
     * of SOME of the patch code.  On paper it's pretty simple as it's basically an insertion, 2 multi opcode replacements (possibly twice) and another insertion.
     * The PawnColumnWorker_Loadout.DoCell() should be kept VERY similar to how RimWorld's code for the other two Column Workers work, that will make maintenance of this
     * a lot easier.  This also borrows information from PawnColumnWorker_Loadout in order to keep a unified UI that requires relatively minimal work to alter.
     */
    static class PawnColumnWorkers_SwapButtons
    {
        static readonly string logPrefix = Assembly.GetExecutingAssembly().GetName().Name + " :: " + typeof(PawnColumnWorkers_SwapButtons).Name + " :: ";

        // NOTE: For the two strings below you can also change the translation string...
        private const string apparelString = "CE_Outfits"; // change this to change the word after "Edit" in the edit tooltip for outfits.
        private const string drugString = "CE_Drugs"; // change this to change the word after "Edit" in the edit tooltip for drug policies.

        /// <summary>
        /// Handles the patch code.
        /// </summary>
        public static void Patch()
        {
            Type[] targetTypes = new Type[] { typeof(PawnColumnWorker_Outfit), typeof(PawnColumnWorker_DrugPolicy) };
            string targetName = "DoCell";
            HarmonyMethod transpiler = new HarmonyMethod(AccessTools.Method(typeof(PawnColumnWorkers_SwapButtons), "Transpiler"));

            for (int i = 0; i < targetTypes.Length; i++)
            {
                MethodBase method = targetTypes[i].GetMethod(targetName, AccessTools.all);
                HarmonyBase.instance.Patch(method, null, null, (HarmonyMethod)transpiler);
            }
        }

        /* Order of patching (Refer to PawnColumnWorker_Loadout for variable name references and overall structure of code):
         * 01 - Change the line:
         *  int num = Mathf.FloorToInt((rect.width - 4f) * 0.714285731f);
         * to
         *  int num = Mathf.FloorToInt((rect.width - 4f) - IconSize);
         * By swaping the multiply instruction out for subtract and one float constant for another.
         * 
         * 02 - Change the line:
         *  int num2 = Mathf.FloorToInt((rect.width - 4f) * 0.2857143f);
         * to
         *  int num2 = Mathf.FloorToInt(IconSize);
         * By replacing all the instructions that generate the value before FloorToInt with a single float constant.
         * 
         * 03 - Insert the line:
         *  float num4 = rect.y + ((rect.height - IconSize) / 2);
         * By inserting a new local variable with that math after num3 equivelent is created.
         * 
         * 04 - This CAN happen twice in the same way so these patch steps are writen differently from the other steps.
         *    - Change the line:
         *  Rect forcedHoldRect = new Rect(num3, rect.y + 2f, (float)num2, rect.height - 4f);
         * to
         *  Rect forcedHoldRect = new Rect(num3, num4, (float)num2, (float)num2);
         * By replacing a block of instructions that produced the parameters for the constructor with a new block of (simpler) instructions setting up different parameters.
         * 
         * 05 - Turns out this also happens twice...
         *    - Change the line:
         *  if (Widgets.ButtonText(assignTabRect, "AssignTabEdit".Translate(), true, false, true))
         * to
         *  if (Widgets.ButtonImage(assignTabRect, EditImage))
         * By finding the load of the interesting Rect and replacing all instructions (not including) the end of the sequence.
         * Also need to remember the label of the 'last' time we do this as that's our insertion point for 06.
         * 
         * 06 - The final patch is to insert (only insert) some new code to handle the tooltip for the edit button.
         *      Insert the line:
         *   TooltipHandler.TipRegion(assignTabRect, new TipSignal(textGetter("CE_Loadouts"), pawn.GetHashCode() * 613));
         *  However simpler to insert the line:
         *   PawnColumnWorkers_SwapButtons.DoToolTip(assignTabRect, pawn, "UntranslatedString");
         *  By using the label found in 05 step and inserting our new instructions to generate a tooltip at that label position.
         *  Using the label is important as otherwise the tooltip would only show up when the user clicked on the button.
         */

        // concept here is to replicate the changes in PawnColumnWorker_Loadout.DoCell but in IL.
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il, MethodBase orgMethod)
        {
            ParameterInfo[] args = orgMethod.GetParameters();
            LocalBuilder[] locals = HarmonyBase.GetLocals(il);

            int patchPhase = 0;
            bool writeInstructions = true;

            // Find the index of the arg (parameter) that has the expected type (Rect)
            byte argRectIndex = 0;
            int argPawnIndex = 0;
            for (byte i = 0; i < args.Length; i++)
            {
                if (argRectIndex == 0 && args[i].ParameterType.Equals(typeof(Rect)))
                {
                    argRectIndex = i;
                    argRectIndex++; // because we are dealing with a non-static object, arg0 is the instance but our array doesn't include the instance as the first element.
                }
                if (argPawnIndex == 0 && args[i].ParameterType.Equals(typeof(Pawn)))
                {
                    argPawnIndex = i;
                    argPawnIndex++; // because we are dealing with a non-static object...
                }
                if (argRectIndex != 0 && argPawnIndex != 0)
                    break;
            }

            // Find local variables which corespond to num2 (int) and num3 (float).  Also count the number of local variables of type Rect.
            int localNum2Index = -1;
            int localNum3Index = -1;
            int localRect2Index = -1;
            int localRect3Index = -1;
            for (int i = 0, hits=0, localRectCount = 0; i < locals.Length; i++)
            {
                // Num2 should be the second integer found.
                if (localNum2Index < 0 && locals[i].LocalType.Equals(typeof(int)))
                {
                    if (hits == 1)
                    {
                        localNum2Index = i;
                    } else
                    {
                        hits++;
                    }
                }

                // Num2 should be the first Single found.
                if (localNum3Index < 0 && locals[i].LocalType.Equals(typeof(Single)))
                {
                    localNum3Index = i;
                }

                // Count local Rect types found.
                if (locals[i].LocalType.Equals(typeof(Rect)))
                {
                    localRectCount++;
                    if (localRectCount == 2)
                        localRect2Index = Convert.ToByte(i);
                    if (localRectCount == 3)
                        localRect3Index = Convert.ToByte(i);
                }
            }

            // so we can keep track of some previously yielded instructions.
            List<CodeInstruction> previous = new List<CodeInstruction>();
            bool trackPrevious = false;

            // generate a new local variable coresponding to num4.
            LocalBuilder localNum4 = il.DeclareLocal(typeof(float));

            int phaseFinalGraphicsPatch = 16;
            Label? finalInsertionPoint = null;

            foreach (CodeInstruction instruction in instructions)
            {
                // 06 Insert the tooltip code near the end...
                if (patchPhase == phaseFinalGraphicsPatch && !instruction.labels.NullOrEmpty() && finalInsertionPoint.HasValue
                    && instruction.labels.FirstIndexOf(l => l.GetHashCode().Equals(finalInsertionPoint.Value.GetHashCode())) >= 0)
                {
                    // first pull the labels off of this instruction...
                    List<Label> copiedLabels = new List<Label>(instruction.labels);
                    instruction.labels.Clear();
                    // insert the bottom most Rect local variable instruction as well as transfering the labels to the instruction.
                    CodeInstruction insInstruction = new CodeInstruction(OpCodes.Ldloc_S, (localRect3Index < 0 ? localRect2Index : localRect3Index));
                    insInstruction.labels.AddRange(copiedLabels);
                    yield return insInstruction;
                    // insert the load argument (Pawn pawn) instruction.
                    yield return new CodeInstruction(OpCodes.Ldarg, argPawnIndex);
                    // insert the new string constant instruction (depends on some state information for which string).
                    string insString = "";
                    if (localRect3Index < 0)
                    {
                        insString = drugString;
                    } else
                    {
                        insString = apparelString;
                    }
                    yield return new CodeInstruction(OpCodes.Ldstr, insString);
                    // insert the call to make the tooltip.
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PawnColumnWorkers_SwapButtons), "DoToolTip"));
                    patchPhase = 17;
                }

                // 05 Now we need to find the branch false instruction and insert our new instructions (as well as resume writing instructions out).
                // This is a pretty important block since it controls the flow of patching, pay special attention to changes that alter this.
                if ((patchPhase == 10 || patchPhase == 15) && instruction.opcode.Equals(OpCodes.Brfalse))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_S, (patchPhase == 10 ? locals[localRect2Index] : locals[localRect3Index]));
                    if (patchPhase == 10 && localRect3Index >= 0)
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Property(typeof(PawnColumnWorker_Loadout), "ClearImage").GetGetMethod());
                    else
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Property(typeof(PawnColumnWorker_Loadout), "EditImage").GetGetMethod());
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Widgets), "ButtonImage", new Type[] { typeof(Rect), typeof(Texture2D) }));
                    patchPhase++;
                    if (localRect3Index < 0)
                    {
                        patchPhase = phaseFinalGraphicsPatch;
                    } else if (patchPhase != phaseFinalGraphicsPatch)
                    {
                        trackPrevious = true;
                    }
                    if (patchPhase == phaseFinalGraphicsPatch)
                    {
                        finalInsertionPoint = instruction.operand as Label?;
                    }
                    writeInstructions = true;
                }

                // 05 First we need to find the local variable load instruction for one of our interesting Rect types.  Stop writing instructions.
                if ((patchPhase == 9 && instruction.opcode.Equals(OpCodes.Ldloc_S) && (instruction.operand as LocalVariableInfo).LocalIndex.Equals(localRect2Index)) ||
                    (patchPhase == 14 && instruction.opcode.Equals(OpCodes.Ldloc_S) && (instruction.operand as LocalVariableInfo).LocalIndex.Equals(localRect3Index)))
                {
                    patchPhase++;
                    writeInstructions = false;
                }

                // 04 Finally when we find the constructor call insert our arguments to the constructor.
                if ((patchPhase == 8 || patchPhase == 13) && instruction.opcode.Equals(OpCodes.Call) && HarmonyBase.doCast((instruction.operand as ConstructorInfo)?.DeclaringType.Equals(typeof(Rect))))
                {
                    // num3 (technically we could have preserved this instruction earlier...
                    yield return new CodeInstruction(OpCodes.Ldloc, localNum3Index);
                    // num4
                    yield return new CodeInstruction(OpCodes.Ldloc, localNum4);
                    // num2 as float (twice)
                    yield return new CodeInstruction(OpCodes.Ldloc, localNum2Index);
                    yield return new CodeInstruction(OpCodes.Conv_R4);
                    yield return new CodeInstruction(OpCodes.Ldloc, localNum2Index);
                    yield return new CodeInstruction(OpCodes.Conv_R4);
                    writeInstructions = true;
                    patchPhase++;
                }

                // 04 Next we locate the load index for Num3.
                if ((patchPhase == 7 || patchPhase == 12) && HarmonyBase.OpcodeLoadIndex(instruction) == localNum3Index)
                {
                    writeInstructions = false;
                    patchPhase++; // phase to 8 and 13
                }

                // 04 First we are looking for a series of 3 instructions, load constant float 4f, add, store to num3.
                if ((patchPhase == 6 || patchPhase == 11) && HarmonyBase.OpcodeStoreIndex(instruction) == localNum3Index)
                {
                    int lastIndex = previous.Count - 1;
                    if (previous[lastIndex - 1].opcode.Equals(OpCodes.Ldc_R4) && (previous[lastIndex - 1].operand as float?).GetValueOrDefault().Equals(4f)
                        && previous[lastIndex].opcode.Equals(OpCodes.Add))
                    {
                        previous.Clear();
                        trackPrevious = false;
                        patchPhase++; // phase to 7 and 8
                    }
                }


                // 03 Now we insert our new local variable code since 'num3' has just been stored.
                if (patchPhase == 5)
                {
                    // hint: Math: rect.y + ((rect.height - IconSize) / 2)
                    // get rect.y
                    yield return new CodeInstruction(OpCodes.Ldarga_S, argRectIndex);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Property(typeof(Rect), "y").GetGetMethod());
                    // get the rect.height
                    yield return new CodeInstruction(OpCodes.Ldarga_S, argRectIndex);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Property(typeof(Rect), "height").GetGetMethod());
                    // get IconSize
                    yield return new CodeInstruction(OpCodes.Ldc_R4, PawnColumnWorker_Loadout.IconSize);
                    // do subtraction
                    yield return new CodeInstruction(OpCodes.Sub);
                    // get the divisor
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 2f);
                    // do division
                    yield return new CodeInstruction(OpCodes.Div);
                    // do addition
                    yield return new CodeInstruction(OpCodes.Add);
                    // store the local variable
                    yield return new CodeInstruction(OpCodes.Stloc, localNum4);
                    trackPrevious = true;
                    patchPhase = 6;
                }

                // 03 First we search for another stloc instruction (probably _2) and go to the next phase... (we want to keep the store intact).
                if (patchPhase == 4 && HarmonyBase.OpcodeStoreIndex(instruction) > -1)
                {
                    patchPhase = 5;
                }

                // 02 Now we locate the call to FloorToInt(), insert our constant and resume allowing instructions out.
                if (patchPhase == 3 && instruction.opcode.Equals(OpCodes.Call)
                    && HarmonyBase.doCast((instruction.operand as MethodInfo)?.DeclaringType.Equals(typeof(Mathf)))
                    && HarmonyBase.doCast((instruction.operand as MethodInfo)?.Name.Equals("FloorToInt")))
                {
                    yield return new CodeInstruction(OpCodes.Ldc_R4, PawnColumnWorker_Loadout.IconSize);
                    writeInstructions = true;
                    patchPhase = 4;
                }

                // 02 First we locate the instruction that loads the Rect type arg and stop allowing instructions out.
                if (patchPhase == 2 && instruction.opcode.Equals(OpCodes.Ldarga_S) && (instruction.operand as byte?).HasValue
                    && (instruction.operand as byte?).Value.Equals(argRectIndex))
                {
                    writeInstructions = false;
                    patchPhase = 3;
                }

                // 01 Next we find the multiply instruction and swap it out for a subtraction.
                if (patchPhase == 1 && instruction.opcode.Equals(OpCodes.Mul))
                {
                    instruction.opcode = OpCodes.Sub;
                    patchPhase = 2;
                }

                // 01 First, locate the big float value...
                if (patchPhase == 0 && instruction.opcode.Equals(OpCodes.Ldc_R4) && HarmonyBase.doCast((instruction.operand as float?).Value < 1))
                {
                    instruction.operand = PawnColumnWorker_Loadout.IconSize;
                    patchPhase = 1;
                }

                if (trackPrevious)
                    previous.Add(instruction);

                if (writeInstructions)
                    yield return instruction;
            }
        }

        /// <summary>
        /// This is a helper method inserted by the transpiler above.  This handles creating the tooltip since doing that in IL would be hard for others to maintain.
        /// </summary>
        /// <param name="area">Rect type which defines the area of the tooltip, same as the button graphic area.</param>
        /// <param name="pawn">Pawn type which is used to identify the tooltip object.</param>
        /// <param name="untranslated">string type which is an untranslated string such as "CE_Outfits" or "CE_Drugs"</param>
        /// <remarks>Makes direct use of PawnColumnWorker_Loadout.textGetter()  Because the call to this is being inserted, it must be public.</remarks>
        public static void DoToolTip(Rect area, Pawn pawn, string untranslated)
        {
            TooltipHandler.TipRegion(area, new TipSignal(PawnColumnWorker_Loadout.textGetter(untranslated), pawn.GetHashCode() * 613));
        }
    }
}