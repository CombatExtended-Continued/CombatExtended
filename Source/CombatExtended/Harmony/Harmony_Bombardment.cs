using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(Bombardment), nameof(Bombardment.TryDoExplosion))]
    public static class Bombardment_TryDoExplosion_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler (IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            foreach (var instruction in instructions)
            {  // Replaces the default damage and armor pen values of -1
                if (instruction.opcode == OpCodes.Ldc_I4_M1)
                {
                    yield return new CodeInstruction(OpCodes.Ldc_I4, 546); // new damage
                }
                else if (instruction.opcode == OpCodes.Ldc_R4 &&  (float)instruction.operand == -1f)
                {
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 180f); // new pen
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}
