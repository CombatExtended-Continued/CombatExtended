using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE;
[HarmonyPatch(typeof(Bombardment), nameof(Bombardment.TryDoExplosion))]
public static class Bombardment_TryDoExplosion_Patch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        bool foundInjection = false;
        bool secondInjection = false;
        foreach (var instruction in instructions)
        {  // Replaces the default damage and armor pen values of -1
            if (instruction.opcode == OpCodes.Ldc_I4_M1)
            {
                yield return new CodeInstruction(OpCodes.Ldc_I4, 546); // new damage
                foundInjection = true;
            }
            else if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == -1f)
            {
                yield return new CodeInstruction(OpCodes.Ldc_R4, 180f); // new pen
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
