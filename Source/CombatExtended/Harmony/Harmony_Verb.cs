﻿using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;
using RimWorld;
using System.Reflection.Emit;
using System.Reflection;

namespace CombatExtended.HarmonyCE;
[HarmonyPatch(typeof(Verb), nameof(Verb.IsStillUsableBy))]
internal static class Harmony_Verb_IsStillUsableBy
{
    internal static void Postfix(Verb __instance, ref bool __result, Pawn pawn)
    {
        if (__result)
        {
            var tool = __instance.tool as ToolCE;
            if (tool != null)
            {
                __result = tool.restrictedGender == Gender.None || tool.restrictedGender == pawn.gender;
            }
        }
    }
}

[HarmonyPatch(typeof(Verb), nameof(Verb.TryCastNextBurstShot))]
internal static class Harmony_Verb_TryCastNextBurstShot
{
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        List<CodeInstruction> codes = instructions.ToList();

        FieldInfo fverbProps = AccessTools.Field(typeof(Verb), nameof(Verb.verbProps));
        FieldInfo fticksBetweenBurstShots = AccessTools.Field(typeof(VerbProperties), nameof(VerbProperties.ticksBetweenBurstShots));

        MethodInfo pnonInterruptingSelfCast = AccessTools.PropertyGetter(typeof(Verb), nameof(Verb.NonInterruptingSelfCast));
        MethodInfo pCasterPawn = AccessTools.PropertyGetter(typeof(Verb), nameof(Verb.CasterPawn));
        MethodInfo pSpawned = AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.Spawned));
        bool foundInjection = false;

        bool ticksBetweenBurstShotsFinished = false;

        for (int i = 0; i < codes.Count; i++)
        {
            if (!ticksBetweenBurstShotsFinished)
            {
                if (codes[i].LoadsField(fverbProps) && codes[i + 1].LoadsField(fticksBetweenBurstShots))
                {
                    ticksBetweenBurstShotsFinished = true;
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_Verb_TryCastNextBurstShot), nameof(GetTicksBetweenBurstShots)));
                    i++;
                    continue;
                }
            }

            // Check if the caster pawn is spawned before attempting to set them into cooldown stance,
            // since the caster may be dead as a result e.g. a successful melee riposte in TryCastShot().
            // Vanilla already performs this check if the attack connected, but not when it missed.
            if (codes[i].Branches(out Label? blockEndLabel) && codes[i - 1].Is(OpCodes.Call, pnonInterruptingSelfCast))
            {
                yield return codes[i];
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Callvirt, pCasterPawn);
                yield return new CodeInstruction(OpCodes.Callvirt, pSpawned);
                yield return new CodeInstruction(OpCodes.Brfalse_S, blockEndLabel);
                foundInjection = true;
                continue;
            }
            yield return codes[i];
        }
        if (!foundInjection)
        {
            Log.Error($"Combat Extended :: Failed to find injection point when applying Patch: {HarmonyBase.GetClassName(MethodBase.GetCurrentMethod()?.DeclaringType)}");
        }
    }

    private static int GetTicksBetweenBurstShots(Verb verb)
    {
        float ticksBetweenBurstShots = verb.verbProps.ticksBetweenBurstShots;
        WeaponPlatform platform = verb.EquipmentSource as WeaponPlatform;
        if (verb is Verb_LaunchProjectileCE && platform != null)
        {
            float modified = platform.GetStatValue(CE_StatDefOf.TicksBetweenBurstShots);
            if (modified > 0)
            {
                ticksBetweenBurstShots = modified;
            }
        }
        return (int)ticksBetweenBurstShots;
    }
}
