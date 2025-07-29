using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using WeaponProficiency.Patches;

namespace CombatExtended.Compatibility.WeaponProficiencyCompat
{
    [HarmonyPatch(typeof(Pawn_HealthTracker_Notify_UsedVerb_WeaponProficiencyPatch), MethodType.Constructor)]
    [HarmonyPatch(MethodType.Normal)]
    internal static class Harmony_VerbLaunchProjectilePatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var verbLaunchType = typeof(Verb_LaunchProjectile);
            var verbLaunchCEType = typeof(Verb_LaunchProjectileCE);

            for (int i = 0; i < codes.Count; i++)
            {
                // Replace "is Verb_LaunchProjectile" with "is Verb_LaunchProjectile || is Verb_LaunchProjectileCE"
                if (codes[i].opcode == OpCodes.Isinst && codes[i].operand as Type == verbLaunchType)
                {
                    // Insert additional check for Verb_LaunchProjectileCE
                    yield return codes[i]; // isinst Verb_LaunchProjectile
                    yield return new CodeInstruction(OpCodes.Dup); // duplicate result
                    var labelNext = codes[i].labels.Count > 0 ? codes[i].labels[0] : default; // Simplified 'default' expression
                    yield return new CodeInstruction(OpCodes.Brtrue_S, labelNext); // if true, skip next
                    yield return new CodeInstruction(OpCodes.Pop); // pop null
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // load verb
                    yield return new CodeInstruction(OpCodes.Isinst, verbLaunchCEType); // isinst Verb_LaunchProjectileCE
                }
                else
                {
                    yield return codes[i];
                }
            }
        }
    }
}
