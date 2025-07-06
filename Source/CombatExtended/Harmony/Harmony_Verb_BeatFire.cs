using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace CombatExtended.HarmonyCE;
class Harmony_Verb_BeatFire
{
    [HarmonyPatch(typeof(Verb_BeatFire), "TryCastShot")]
    class EditFirePunchingStrength
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool foundInjection = false;
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && instruction.operand is float amount && amount == 32f)
                {
                    instruction.operand = 48f;
                    foundInjection = true;
                }
                yield return instruction;
            }
            if (!foundInjection)
            {
                Log.Error($"Combat Extended :: Failed to find injection point when applying Patch: {HarmonyBase.GetClassName(MethodBase.GetCurrentMethod()?.DeclaringType)}");
            }
        }
    }
}
