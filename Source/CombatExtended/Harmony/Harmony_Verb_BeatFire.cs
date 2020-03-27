using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using Verse;

namespace CombatExtended.HarmonyCE
{
    class Harmony_Verb_BeatFire
    {
        [HarmonyPatch(typeof(Verb_BeatFire), "TryCastShot")]
        class EditFirePunchingStrength
        {
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var instruction in instructions)
                {
                    if (instruction.opcode == OpCodes.Ldc_R4 && instruction.operand is float amount && amount == 32f)
                    {
                        instruction.operand = 48f;
                    }
                    yield return instruction;
                }
            }
        }
    }
}
