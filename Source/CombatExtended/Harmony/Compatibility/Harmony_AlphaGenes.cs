using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace CombatExtended.HarmonyCE.Compatibility
{

    public static class Harmony_AlphaGenes
    {
        private static Type TypeOfCanCommandTo_Patch_HarmonyPatches
        {
            get
            {
                return AccessTools.TypeByName("AlphaGenes.AlphaGenes_Pawn_MechanitorTracker_CanCommandTo_Patch");
            }
        }
        private static Type TypeOfDrawCommandRadius_Patch_HarmonyPatches
        {
            get
            {
                return AccessTools.TypeByName("AlphaGenes.AlphaGenes_Pawn_MechanitorTracker_DrawCommandRadius_Patch");
            }
        }
        [HarmonyPatch]
        public static class Harmony_CanCommandTo_Patch
        {
            public static bool Prepare()
            {
                return TypeOfCanCommandTo_Patch_HarmonyPatches != null;
            }

            public static MethodBase TargetMethod()
            {
                return AccessTools.Method("AlphaGenes.AlphaGenes_Pawn_MechanitorTracker_CanCommandTo_Patch:ModifyRange");
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                foreach (var instruction in instructions)
                {
                    if (instruction.opcode == OpCodes.Ldc_R4 && ((float)instruction.operand) == 1225f)
                    {
                        yield return new CodeInstruction(OpCodes.Ldc_R4, 3469.21f); //Your value here
                    }
                    else if (instruction.opcode == OpCodes.Ldc_R4 && ((float)instruction.operand) == 225f)
                    {
                        yield return new CodeInstruction(OpCodes.Ldc_R4, 835.21f); //Your value here
                    }
                    else
                    {
                        yield return instruction;
                    }
                }
            }
        }

        [HarmonyPatch]
        public static class Harmony_DrawCommandRadius_Patch
        {
            public static bool Prepare()
            {
                return TypeOfDrawCommandRadius_Patch_HarmonyPatches != null;
            }

            public static MethodBase TargetMethod()
            {
                return AccessTools.Method("AlphaGenes.AlphaGenes_Pawn_MechanitorTracker_DrawCommandRadius_Patch:DrawExtraCommandRadius");
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                foreach (var instruction in instructions)
                {
                    if (instruction.opcode == OpCodes.Ldc_R4 && ((float)instruction.operand) == 35f)
                    {
                        yield return new CodeInstruction(OpCodes.Ldc_R4, 58.9f); //Your value here
                    }
                    else if (instruction.opcode == OpCodes.Ldc_R4 && ((float)instruction.operand) == 15f)
                    {
                        yield return new CodeInstruction(OpCodes.Ldc_R4, 28.9f); //Your value here
                    }
                    else
                    {
                        yield return instruction;
                    }
                }
            }
        }
    }
}
