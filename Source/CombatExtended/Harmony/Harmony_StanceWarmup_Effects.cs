using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace CombatExtended.HarmonyCE;

/// <summary>
/// This should fix twice spawning effects. The idea is to change spawn conditions from
/// 'if (verbProps.soundAiming != null)'
/// to
/// 'if (verbProps.soundAiming != null && <see cref="ShouldSpawnAimingSound(Stance_Warmup)"/>)'
/// </summary>
[HarmonyPatch(typeof(Stance_Warmup), nameof(Stance_Warmup.InitEffects))]
public static class Harmony_StanceWarmup_Effects
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var list = instructions.ToList();
        var soundField = AccessTools.Field(typeof(VerbProperties), nameof(VerbProperties.soundAiming));
        var aimingSoundPatched = false;
        for (var i = 0; i < list.Count; i++)
        {
            var ins = list[i];
            if (!aimingSoundPatched && ins.LoadsField(soundField))
            {
                var brIns = list[i + 1];
                var label = (Label)brIns.operand;
                list.InsertRange(i + 2,
                    new CodeInstruction[] {
                        new CodeInstruction(OpCodes.Ldarg_0),
                        CodeInstruction.Call(typeof(Harmony_StanceWarmup_Effects), nameof(ShouldSpawnAimingSound)),
                        new CodeInstruction(OpCodes.Brfalse_S, label)
                    });
                aimingSoundPatched = true;
            }
        }
        return list;
    }
    private static bool ShouldSpawnAimingSound(Stance_Warmup stance)
    {
        if (stance.verb is Verb_LaunchProjectileCE CEVerb)
        {
            return CEVerb.ShouldSpawnAimingSound;
        }
        return true;
    }
}
