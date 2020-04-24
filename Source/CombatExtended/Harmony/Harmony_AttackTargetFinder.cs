using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(AttackTargetFinder), "BestAttackTarget")]
    internal static class Harmony_AttackTargetFinder_BestAttackTarget
    {
        private static bool EMPOnlyTargetsMechanoids()
        {
            return false;
        }
        
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            for (int i = 0; i < instructions.Count(); i++)
            {
	      if (codes[i].opcode == OpCodes.Call && ReferenceEquals(codes[i].operand, AccessTools.Method(typeof(VerbUtility), nameof(VerbUtility.IsEMP))))
                {
                    codes[i - 2].opcode = OpCodes.Nop;
                    codes[i - 1].opcode = OpCodes.Nop;
                    codes[i].operand = typeof(Harmony_AttackTargetFinder_BestAttackTarget).GetMethod(nameof(EMPOnlyTargetsMechanoids), AccessTools.all);
                    break;
                }
            }
            return codes;
        }
    }
}
