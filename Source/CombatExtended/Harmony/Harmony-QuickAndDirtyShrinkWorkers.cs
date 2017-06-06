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
    static class PatchCoreWorkers
    {
        static readonly string logPrefix = Assembly.GetExecutingAssembly().GetName().Name + " :: " + typeof(PatchCoreWorkers).Name + " :: ";

        public static void Patch()
        {
            Type[] targetTypes = new Type[] { typeof(PawnColumnWorker_Outfit), typeof(PawnColumnWorker_DrugPolicy) };
            string[] targetNames = new string[] { "GetMinWidth", "GetOptimalWidth" };
            HarmonyMethod[] transpilers = new HarmonyMethod[] { new HarmonyMethod(typeof(PatchCoreWorkers), "MinWidth"),
                                                                new HarmonyMethod(typeof(PatchCoreWorkers), "OptWidth")};

            for (int i = 0; i < targetTypes.Length; i++)
            {
                for (int j = 0; j < targetNames.Length; j++)
                {
                    MethodBase method = targetTypes[i].GetMethod(targetNames[j], AccessTools.all);
                    HarmonyBase.instance.Patch(method, null, null, (HarmonyMethod) transpilers[j]);
                }
            }

        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> MinWidth(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4)
                {
                    instruction.operand = PawnColumnWorker_Loadout._MinWidth;
                }
                yield return instruction;
            }
        }
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> OptWidth(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4)
                {
                    instruction.operand = PawnColumnWorker_Loadout._OptimalWidth;
                }
                yield return instruction;
            }
        }
    }
}