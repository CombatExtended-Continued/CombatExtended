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

namespace CombatExtended.HarmonyCE
{
    /* This class handles patching of the Core PawnColumnWorkers that are used on the assign tab which are similar to the PawnColumnWorker_Loadout.
     * This patch reads the variables from PawnColumnWorker_Loadout and sets Core to use the same values so all 3 workers use the same size information.
     * This is relatively straight forward in that it's just replacing some constants.
     */
    static class PawnColumnWorkers_Resize
    {

        static readonly float orgMinWidth = 194f;
        static readonly float orgOptimalWidth = 251f;

        /// <summary>
        /// Handles the patch work as this is more efficiently handled as a complex patch instead of automatically by Harmony.
        /// </summary>
        public static void Patch()
        {
            Type[] targetTypes = {
                typeof(PawnColumnWorker_Outfit),
                typeof(PawnColumnWorker_DrugPolicy),
                typeof(PawnColumnWorker_FoodRestriction)
            };
            string[] targetNames = new string[] { "GetMinWidth", "GetOptimalWidth" };
            HarmonyMethod[] transpilers = new HarmonyMethod[] {
                new HarmonyMethod(typeof(PawnColumnWorkers_Resize), "MinWidth"),
                new HarmonyMethod(typeof(PawnColumnWorkers_Resize), "OptWidth")
            };

            for (int i = 0; i < targetTypes.Length; i++)
            {
                for (int j = 0; j < targetNames.Length; j++)
                {
                    MethodBase method = targetTypes[i].GetMethod(targetNames[j], AccessTools.all);
                    HarmonyBase.instance.Patch(method, null, null, (HarmonyMethod)transpilers[j]);
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

        // NOTE: For the two strings below you can also change the translation string...
        private const string apparelString = "CE_Outfits"; // change this to change the word after "Edit" in the edit tooltip for outfits.
        private const string drugString = "CE_Drugs"; // change this to change the word after "Edit" in the edit tooltip for drug policies.

        /// <summary>
        /// Handles the patch code.
        /// </summary>
        public static void Patch()
        {
            MethodBase[] targetMethods =
            {
                typeof(PawnColumnWorker_Outfit).GetMethod("DoCell", AccessTools.all),
                typeof(DrugPolicyUIUtility).GetMethod("DoAssignDrugPolicyButtons", AccessTools.all),
                typeof(PawnColumnWorker_FoodRestriction).GetMethod("DoAssignFoodRestrictionButtons", AccessTools.all)
            };
            HarmonyMethod transpiler = new HarmonyMethod(AccessTools.Method(typeof(PawnColumnWorkers_SwapButtons), "Transpiler"));

            foreach (var method in targetMethods)
            {
                HarmonyBase.instance.Patch(method, null, null, transpiler);
            }
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();

            var mainButtonIndex = -1;
            var editButtonBlockStart = -1;
            var editButtonBlockEnd = -1;

            var passedFirstRectYCall = false;
            var rectYIndices = new List<int>();

            var editButtonIndices = new List<int>();
            var clearButtonIndices = new List<int>();

            for (var i = 0; i < codes.Count; i++)
            {
                var curCode = codes[i];

                // Populate indices
                if (editButtonBlockStart >= 0 && editButtonBlockEnd < 0 && curCode.opcode == OpCodes.Ldarga_S)
                {
                    editButtonBlockEnd = i - 1;
                }
                if (mainButtonIndex >= 0 && editButtonBlockStart < 0 && curCode.opcode == OpCodes.Ldarga_S)
                {
                    editButtonBlockStart = i;
                }
                if (curCode.opcode == OpCodes.Call && curCode.operand.Equals(typeof(Mathf).GetMethod("FloorToInt", AccessTools.all)) && mainButtonIndex < 0)
                {
                    mainButtonIndex = i - 2;
                }

                // Watch for calls to rect.y + 2f, we replace them with our num4 (see PawnColumnWorker_Loadout)
                if (curCode.opcode == OpCodes.Call &&
                    ReferenceEquals(curCode.operand, typeof(Rect).GetMethod("get_y", AccessTools.all)))
                {
                    if (passedFirstRectYCall && i + 1 < codes.Count && codes[i + 1].opcode == OpCodes.Ldc_R4 && codes[i + 1].operand.Equals(2f))
                    {
                        rectYIndices.Add(i);
                    }
                    else
                    {
                        passedFirstRectYCall = true;
                    }
                }

                // Watch for the beginning of edit/clear forced button sections
                if (curCode.operand?.Equals("ClearForcedApparel") ?? false)
                {
                    clearButtonIndices.Add(i);
                }

                if (curCode.operand?.Equals("AssignTabEdit") ?? false)
                {
                    editButtonIndices.Add(i);
                }
            }

            if (codes[mainButtonIndex].operand.Equals(0.714285731f))
            {
                // Change outfit/drug policy button width to match new edit/clear buttons
                codes[mainButtonIndex].operand = PawnColumnWorker_Loadout.IconSize;
                codes[mainButtonIndex + 1].opcode = OpCodes.Sub;
            }

            // Change edit button width by nulling out the current block up until stloc (don't want to have to mess with local variable tracking, so this lets us use the existing opcode)
            codes[editButtonBlockStart].opcode = OpCodes.Ldc_R4;
            codes[editButtonBlockStart].operand = PawnColumnWorker_Loadout.IconSize;

            // Null out codes to not mess with indices
            foreach (var code in codes.GetRange(editButtonBlockStart + 1, editButtonBlockEnd - editButtonBlockStart - 2))
            {
                code.operand = null;
                code.opcode = OpCodes.Nop;
            }

            // Replace the edit/clear forced buttons with our icons
            foreach (var index in editButtonIndices)
            {
                // Replace string argument with our image
                var code = codes[index];
                code.opcode = OpCodes.Call;
                code.operand = typeof(PawnColumnWorker_Loadout).GetProperty(nameof(PawnColumnWorker_Loadout.EditImage), AccessTools.all)
                    .GetGetMethod();
                // Null out unneeded bool arguments
                foreach (var cur in codes.GetRange(index + 1, 4))
                {
                    cur.operand = null;
                    cur.opcode = OpCodes.Nop;
                }
                // Replace method call ButtonText->ButtonImage
                codes[index + 6].operand = typeof(Widgets).GetMethod(nameof(Widgets.ButtonImage),
                    new[] { typeof(Rect), typeof(Texture2D), typeof(bool) });
            }
            foreach (var index in clearButtonIndices)
            {
                // Replace string argument with our image
                var code = codes[index];
                code.opcode = OpCodes.Call;
                code.operand = typeof(PawnColumnWorker_Loadout).GetProperty(nameof(PawnColumnWorker_Loadout.ClearImage), AccessTools.all)
                    .GetGetMethod();
                // Null out unneeded bool arguments
                foreach (var cur in codes.GetRange(index + 1, 4))
                {
                    cur.operand = null;
                    cur.opcode = OpCodes.Nop;
                }
                // Replace method call ButtonText->ButtonImage
                codes[index + 6].operand = typeof(Widgets).GetMethod(nameof(Widgets.ButtonImage),
                    new[] { typeof(Rect), typeof(Texture2D), typeof(bool) });
            }

            // Replace rect.y + 2f with rect.y + (rect.height - IconSize) / 2
            // Instead of calling rect.y we use it being already on the stack to load it as argument into our helper method
            foreach (var index in rectYIndices)
            {
                var code = codes[index];
                code.opcode = OpCodes.Callvirt;
                code.operand =
                    typeof(PawnColumnWorkers_SwapButtons).GetMethod(nameof(RectHeightHelper), AccessTools.all);

                // Null out the next two ops as we don't need them
                for (var i = 1; i <= 2; i++)
                {
                    var c = codes[index + i];
                    c.operand = null;
                    c.opcode = OpCodes.Nop;
                }

                // We also want to replace one of the following arguments with num2
                // Assume num2 is loaded at index + 3 and the rect call we want to replace is 5-8, we replace 5 with 3, 6 with 4 (for the float conversion) and null 7-8
                code = codes[index + 5];
                code.operand = codes[index + 3].operand;
                code.opcode = codes[index + 3].opcode;

                code = codes[index + 6];
                code.operand = codes[index + 4].operand;
                code.opcode = codes[index + 4].opcode;
                for (var i = 7; i <= 8; i++)
                {
                    var c = codes[index + i];
                    c.operand = null;
                    c.opcode = OpCodes.Nop;
                }
            }

            return codes;
        }

        public static float RectHeightHelper(ref Rect rect)
        {
            return rect.y + (rect.height - PawnColumnWorker_Loadout.IconSize) / 2f;
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
