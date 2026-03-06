using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace CombatExtended.HarmonyCE.Compatibility;

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
            bool foundInjection = false;
            bool secondInjection = false;
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && ((float)instruction.operand) == 1225f)
                {
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 3469.21f); //Your value here
                    foundInjection = true;
                }
                else if (instruction.opcode == OpCodes.Ldc_R4 && ((float)instruction.operand) == 225f)
                {
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 835.21f); //Your value here
                    secondInjection = true;
                }
                else
                {
                    yield return instruction;
                }
            }
            if (!foundInjection)
            {
                Log.Error($"Combat Extended :: Failed to find first injection point when applying Patch: {HarmonyBase.GetClassName(MethodBase.GetCurrentMethod()?.DeclaringType)}");
            }
            if (!secondInjection)
            {
                Log.Error($"Combat Extended :: Failed to find second injection point when applying Patch: {HarmonyBase.GetClassName(MethodBase.GetCurrentMethod()?.DeclaringType)}");
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
            bool foundInjection = false;
            bool secondInjection = false;
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && ((float)instruction.operand) == 35f)
                {
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 58.9f); //Your value here
                    foundInjection = true;
                }
                else if (instruction.opcode == OpCodes.Ldc_R4 && ((float)instruction.operand) == 15f)
                {
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 28.9f); //Your value here
                    secondInjection = true;
                }
                else
                {
                    yield return instruction;
                }
            }
            if (!foundInjection)
            {
                Log.Error($"Combat Extended :: Failed to find first injection point when applying Patch: {HarmonyBase.GetClassName(MethodBase.GetCurrentMethod()?.DeclaringType)}");
            }
            if (!secondInjection)
            {
                Log.Error($"Combat Extended :: Failed to find second injection point when applying Patch: {HarmonyBase.GetClassName(MethodBase.GetCurrentMethod()?.DeclaringType)}");
            }
        }
    }
}
