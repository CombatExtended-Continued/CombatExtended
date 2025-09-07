using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace CombatExtended.HarmonyCE;
[HarmonyPatch(typeof(CompressibilityDecider), "DetermineReferences")]
public class CompressibilityDecider_DetermineReferences
{

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var instructionsList = instructions.ToList();
        var notProjectileLabel = generator.DefineLabel();
        bool foundInjection = false;

        for (int i = 0; i < instructionsList.Count(); i++)
        {
            // Add an instanceof check before the vanilla cast to Projectile and skip projectiles that are not the correct type
            // to avoid crashing the save
            if (instructionsList[i].Is(OpCodes.Castclass, typeof(Projectile)) && instructionsList[i - 2].IsLdloc())
            {
                // Find and mark the loop condition check as our jump target
                for (int j = i + 1; j < instructionsList.Count(); j++)
                {
                    if (instructionsList[j].IsLdloc() && instructionsList[j].OperandIs(instructionsList[i - 2].operand))
                    {
                        if (instructionsList[j + 1].opcode == OpCodes.Ldc_I4_1 && instructionsList[j + 2].opcode == OpCodes.Add)
                        {
                            instructionsList[j].labels.Add(notProjectileLabel);
                            break;
                        }
                    }
                }
                yield return new CodeInstruction(OpCodes.Isinst, typeof(Projectile));
                yield return new CodeInstruction(OpCodes.Brfalse_S, notProjectileLabel);
                yield return instructionsList[i - 3].Clone(); // ldloc
                yield return instructionsList[i - 2].Clone(); // ldloc
                yield return instructionsList[i - 1].Clone(); // callvirt
                foundInjection = true;
            }

            yield return instructionsList[i];
        }
        if (!foundInjection)
        {
            Log.Error($"Combat Extended :: Failed to find injection point when applying Patch: {HarmonyBase.GetClassName(MethodBase.GetCurrentMethod()?.DeclaringType)}");
        }
    }
}
