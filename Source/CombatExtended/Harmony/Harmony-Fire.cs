using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using Harmony;
using Verse;
using RimWorld;

namespace CombatExtended.Harmony
{
    [HarmonyPatch(typeof(Fire), "DoFireDamage")]
    internal static class Harmony_Fire
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach(CodeInstruction code in instructions)
            {
                if (code.opcode == OpCodes.Ldc_R4 && code.operand is float && (float)code.operand == 150f)
                {
                    code.operand = 300f;
                }
                yield return code;
            }
        }
    }
}
