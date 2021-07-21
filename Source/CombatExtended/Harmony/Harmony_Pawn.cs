using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE
{
    /*
     * Patch for Pawn.SpecialDisplayStats inorder to remove the darkness stats since they are redundent
     */
    [HarmonyPatch]
    public static class Harmony_Pawn
    {
        private const string targetMethod = "MoveNext";

        private const string targetType = "SpecialDisplayStats";

        private static MethodBase mIdeologyActive_Get = AccessTools.PropertyGetter(typeof(ModsConfig), nameof(ModsConfig.IdeologyActive));

        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Pawn).GetNestedTypes(AccessTools.all).First(t => t.Name.Contains(targetType)), targetMethod);
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.opcode == OpCodes.Call && code.OperandIs(mIdeologyActive_Get))
                {
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0).MoveLabelsFrom(code).MoveBlocksFrom(code);
                    continue;
                }
                yield return code;
            }
        }
    }
}
