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

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = instructions.ToList();
            var notProjectileLabel = generator.DefineLabel();

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
                }

                yield return instructionsList[i];
            }
        }
    }
}
