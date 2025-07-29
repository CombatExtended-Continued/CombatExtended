using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using WeaponProficiency.Patches;

namespace CombatExtended.Compatibility.WeaponProficiencyCompat
{
    [HarmonyPatch(typeof(Pawn_HealthTracker_Notify_UsedVerb_WeaponProficiencyPatch), MethodType.Constructor)]
    [HarmonyPatch(MethodType.Normal)]
    internal static class Harmony_VerbLaunchProjectilePatch_Copy
    {
        private static bool Comparer(Verb verb)
        {
            return verb is Verb;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            bool FoundIsInst = false;
            bool DoPatch = false;
            bool blockFlag = false;
            object operand = null;
            foreach (CodeInstruction instruction in instructions)
            {
                if (DoPatch)
                {
                    DoPatch = false;
                    yield return new(OpCodes.Ldarg_1);
                    yield return new(OpCodes.Call, AccessTools.Method(typeof(Harmony_VerbLaunchProjectilePatch_Copy), "Comparer"));
                    CodeInstruction codeInstruction = new(OpCodes.Brtrue_S, operand);
                }
                if (FoundIsInst)
                {
                    if (instruction.opcode == OpCodes.Brtrue_S)
                    {
                        DoPatch = true;
                        FoundIsInst = false;
                        operand = instruction.operand;
                    }
                }
                if (!blockFlag && instruction.opcode == OpCodes.Isinst && instruction.operand.GetType() == typeof(Verb_MeleeAttack))
                {
                    FoundIsInst = true;
                    blockFlag = true;
                }
                yield return instruction;
            }
        }
    }
}