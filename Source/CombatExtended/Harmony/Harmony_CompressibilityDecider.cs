using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(CompressibilityDecider), "DetermineReferences")]
    public class CompressibilityDecider_DetermineReferences
    {
        enum Stage { Searching, Deleting, Done }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var stage = Stage.Searching;
            var instructionsList = instructions.ToList();

            for (int i = 0; i < instructionsList.Count(); i++)
            {
                if (stage == Stage.Searching && IsStartOfProjectileLoopSection(instructionsList, i))
                {
                    instructionsList[i - 3].opcode = OpCodes.Nop;
                    instructionsList[i - 2].opcode = OpCodes.Nop;
                    instructionsList[i - 1].opcode = OpCodes.Nop;
                    instructionsList[i].opcode = OpCodes.Nop;
                    stage = Stage.Deleting;
                }
                else if (stage == Stage.Deleting)
                {
                    if (instructionsList[i].opcode == OpCodes.Blt_S)
                    {
                        stage = Stage.Done;
                    }
                    instructionsList[i].opcode = OpCodes.Nop;
                }

            }
            if (stage != Stage.Done) throw new Exception("CE failed to patch CompressibilityDecider:DetermineReferences");
            return instructionsList;
        }

        private static bool IsStartOfProjectileLoopSection(List<CodeInstruction> instructionsList, int i)
        {
            return instructionsList[i].opcode == OpCodes.Ldc_I4_S
                && (sbyte)instructionsList[i].operand == 49
                && instructionsList[i + 1].opcode == OpCodes.Callvirt
                && instructionsList[i + 1].operand as MethodInfo != null
                && (instructionsList[i + 1].operand as MethodInfo).Name == "ThingsInGroup"
                && instructionsList[i + 2].opcode == OpCodes.Stloc_1;
        }
    }
}
