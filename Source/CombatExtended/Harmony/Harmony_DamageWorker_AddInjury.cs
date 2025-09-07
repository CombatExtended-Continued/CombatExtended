using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;

namespace CombatExtended.HarmonyCE;
[HarmonyPatch(typeof(DamageWorker_AddInjury), "ApplyDamageToPart")]
internal static class Harmony_DamageWorker_AddInjury_ApplyDamageToPart
{
    private static bool _applyingSecondary = false;
    private static bool shieldAbsorbed = false;
    private static readonly int[] ArmorBlockNullOps = { 1, 3, 4, 5, 6 };  // Lines in armor block that need to be nulled out

    private static void ArmorReroute(Pawn pawn, ref DamageInfo dinfo, out bool deflectedByArmor, out bool diminishedByArmor)
    {
        var newDinfo = ArmorUtilityCE.GetAfterArmorDamage(dinfo, pawn, dinfo.HitPart, out deflectedByArmor, out diminishedByArmor, out shieldAbsorbed);
        if (dinfo.HitPart != newDinfo.HitPart)
        {
            if (pawn.Spawned)
            {
                LessonAutoActivator.TeachOpportunity(CE_ConceptDefOf.CE_ArmorSystem, OpportunityType.Critical);    // Inform the player about armor deflection
            }
        }
        Patch_CheckDuplicateDamageToOuterParts.lastHitPartHealth = pawn.health.hediffSet.GetPartHealth(newDinfo.HitPart);

        dinfo = newDinfo;
    }

    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {

        var codes = instructions.ToList();

        // Find armor block
        var armorBlockEnd = codes.FirstIndexOf(c => ReferenceEquals(c.operand, typeof(ArmorUtility).GetMethod("GetPostArmorDamage", AccessTools.all)));

        int armorBlockStart = -1;

        for (int i = armorBlockEnd; i > 0; i--)
        {
            // Find OpCode loading up first argument for GetPostArmorDamage (Pawn)
            if (codes[i].opcode == OpCodes.Ldarg_2)
            {
                armorBlockStart = i;
                break;
            }
        }
        if (armorBlockStart == -1)
        {
            Log.Error("CE failed to transpile DamageWorker_AddInjury: could not identify armor block start");
            return instructions;
        }

        // Replace armor block with our new instructions
        var armorCodes = codes.GetRange(armorBlockStart, armorBlockEnd - armorBlockStart);

        foreach (var index in ArmorBlockNullOps)
        {
            armorCodes[index].opcode = OpCodes.Nop;
            armorCodes[index].operand = null;
        }

        // Override armor method call
        codes[armorBlockEnd].operand = typeof(Harmony_DamageWorker_AddInjury_ApplyDamageToPart).GetMethod(nameof(ArmorReroute), AccessTools.all);

        // Prevent vanilla code from overriding changed damageDef
        codes[armorBlockEnd + 3] = new CodeInstruction(OpCodes.Call, typeof(DamageInfo).GetMethod($"get_{nameof(DamageInfo.Def)}"));
        codes[armorBlockEnd + 4] = new CodeInstruction(OpCodes.Stloc_S, 4);

        // Our method returns a Dinfo instead of float, we want to insert a call to Dinfo.Amount before stloc at ArmorBlockEnd+1
        codes.InsertRange(armorBlockEnd + 1, new[]
        {
            new CodeInstruction(OpCodes.Ldarga_S, 1),
            new CodeInstruction(OpCodes.Call, typeof(DamageInfo).GetMethod($"get_{nameof(DamageInfo.Amount)}"))
        });

        return codes;
    }

    internal static void Postfix(DamageInfo dinfo, Pawn pawn)
    {
        if (shieldAbsorbed)
        {
            return;
        }

        if (dinfo.Weapon?.projectile is ProjectilePropertiesCE props && !props.secondaryDamage.NullOrEmpty() && !_applyingSecondary)
        {
            try
            {
                _applyingSecondary = true;
                foreach (var sec in props.secondaryDamage)
                {
                    if (pawn.Dead || !Rand.Chance(sec.chance))
                    {
                        break;
                    }
                    var secDinfo = sec.GetDinfo(dinfo);

                    pawn.TakeDamage(secDinfo);
                }
            }
            finally
            {
                _applyingSecondary = false;
            }
        }
    }
}

static class Patch_CheckDuplicateDamageToOuterParts
{
    public static float lastHitPartHealth = 0;

    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(DamageWorker_AddInjury), "CheckDuplicateDamageToOuterParts")]
    static class Patch_DamageWorker_AddInjury
    {
        [HarmonyPrefix]
        static bool Prefix(DamageWorker_AddInjury __instance, DamageInfo dinfo, Pawn pawn, float totalDamage, DamageWorker.DamageResult result)
        {
            CE_Utility.DamageOutsideSquishy(__instance, dinfo, pawn, totalDamage, result, lastHitPartHealth);
            return true;
        }
    }

    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(DamageWorker_Cut), "ApplySpecialEffectsToPart")]
    static class Patch_DamageWorker_Cut
    {
        [HarmonyPrefix]
        static bool Prefix(DamageWorker_Cut __instance, DamageInfo dinfo, Pawn pawn, float totalDamage, DamageWorker.DamageResult result)
        {
            CE_Utility.DamageOutsideSquishy(__instance, dinfo, pawn, totalDamage, result, lastHitPartHealth);
            return true;
        }

        // Replaces loop for cleave damage to properly have armor be checked for the cleave target parts.
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var finalizeAddInjury = AccessTools.Method(typeof(DamageWorker_AddInjury), "FinalizeAndAddInjury", new[]
            {
                typeof(Pawn), typeof(float), typeof(DamageInfo), typeof(DamageWorker.DamageResult)
            });
            var getItem = AccessTools.PropertyGetter(typeof(List<BodyPartRecord>), "Item");
            var setHitPart = AccessTools.Method(typeof(DamageInfo), "SetHitPart");
            var callHelper = AccessTools.Method(typeof(Patch_DamageWorker_Cut), "ApplyCECleavedDamage");
            bool foundInjection = false;

            for (int i = 0; i < code.Count - 12; i++)
            {
                if (
                    code[i].opcode == OpCodes.Stloc_S &&
                    code[i + 1].opcode == OpCodes.Ldloca_S &&
                    code[i + 4].Calls(getItem) &&
                    code[i + 5].Calls(setHitPart) &&
                    code[i + 11].Calls(finalizeAddInjury)
                )
                {
                    code.RemoveRange(i, 12);
                    var injected = new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Ldarg_3),
                        new CodeInstruction(OpCodes.Ldloc_S, 6),
                        new CodeInstruction(OpCodes.Ldloc_S, 9),
                        new CodeInstruction(OpCodes.Callvirt, getItem),
                        new CodeInstruction(OpCodes.Ldarg_S, 4),
                        new CodeInstruction(OpCodes.Ldloc_S, 7),
                        new CodeInstruction(OpCodes.Call, callHelper)
                    };
                    code.InsertRange(i, injected);
                    foundInjection = true;
                    break;
                }
            }
            if (!foundInjection)
            {
                Log.Error($"Combat Extended :: Failed to find injection point when applying Patch: {HarmonyBase.GetClassName(MethodBase.GetCurrentMethod()?.DeclaringType)}");
            }
            return code;
        }

        public static void ApplyCECleavedDamage(DamageWorker instance, Pawn pawn, DamageInfo originalDinfo, BodyPartRecord part, DamageWorker.DamageResult result, float cleaveDamage)
        {
            if (originalDinfo.HitPart == part)
            {
                ((DamageWorker_AddInjury)instance).FinalizeAndAddInjury(pawn, cleaveDamage, originalDinfo, result);
                return;
            }
            DamageInfo clone = originalDinfo;
            clone.SetAmount(cleaveDamage);
            DamageInfo newDinfo = ArmorUtilityCE.GetAfterArmorDamage(clone, pawn, part, out bool deflected, out bool diminished, out bool shieldAbsorbed);
            newDinfo.SetHitPart(part);
            if (newDinfo.Amount > 0)
            {
                ((DamageWorker_AddInjury)instance).FinalizeAndAddInjury(pawn, newDinfo.Amount, newDinfo, result);
            }
        }
    }

    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(DamageWorker_Stab), "ApplySpecialEffectsToPart")]
    static class Patch_DamageWorker_Stab
    {
        [HarmonyPrefix]
        static bool Prefix(DamageWorker_Stab __instance, DamageInfo dinfo, Pawn pawn, float totalDamage, DamageWorker.DamageResult result)
        {
            CE_Utility.DamageOutsideSquishy(__instance, dinfo, pawn, totalDamage, result, lastHitPartHealth);
            return true;
        }
    }

    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(DamageWorker_Scratch), "ApplySpecialEffectsToPart")]
    static class Patch_DamageWorker_Scratch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var finalizeAddInjury = AccessTools.Method(typeof(DamageWorker_AddInjury), "FinalizeAndAddInjury", new[]
            {
                typeof(Pawn), typeof(float), typeof(DamageInfo), typeof(DamageWorker.DamageResult)
            });
            var setHitPart = AccessTools.Method(typeof(DamageInfo), nameof(DamageInfo.SetHitPart));
            var callHelper = AccessTools.Method(typeof(Patch_DamageWorker_Scratch), "ApplyCEScratchedDamage");
            bool foundInjection = false;
            for (int i = 0; i < code.Count - 15; i++)
            {
                if (code[i].opcode == OpCodes.Ldloc_0 &&
                    code[i + 1].opcode == OpCodes.Ldfld &&
                    code[i + 2].opcode == OpCodes.Stloc_S &&
                    code[i + 3].opcode == OpCodes.Ldloca_S &&
                    code[i + 5].Calls(setHitPart)
                )

                {
                    int j = i;
                    while (j < code.Count && !code[j].Calls(finalizeAddInjury))
                    {
                        j++;
                    }
                    if (j < code.Count && code[j + 1].opcode == OpCodes.Pop)
                    {
                        int count = (j + 2) - i;
                        code.RemoveRange(i, count);
                        var injected = new List<CodeInstruction>
                        {
                            new CodeInstruction(OpCodes.Ldarg_0),
                            new CodeInstruction(OpCodes.Ldarg_1),
                            new CodeInstruction(OpCodes.Ldarg_3),
                            new CodeInstruction(OpCodes.Ldloc_S, 7),
                            new CodeInstruction(OpCodes.Ldarg_S, 4),
                            new CodeInstruction(OpCodes.Ldarg_2),
                            new CodeInstruction(OpCodes.Call, callHelper)
                        };
                        code.InsertRange(i, injected);
                        foundInjection = true;
                        break;
                    }
                }
            }
            if (!foundInjection)
            {
                Log.Error($"Combat Extended :: Failed to find injection point when applying Patch: {HarmonyBase.GetClassName(MethodBase.GetCurrentMethod()?.DeclaringType)}");
            }
            return code;
        }
        public static void ApplyCEScratchedDamage(DamageWorker instance, Pawn pawn, DamageInfo originalDinfo, BodyPartRecord secondPart, DamageWorker.DamageResult result, float scratchDamage)
        {
            DamageInfo clone = originalDinfo;
            clone.SetAmount(scratchDamage * instance.def.scratchSplitPercentage);
            DamageInfo newDinfo = ArmorUtilityCE.GetAfterArmorDamage(clone, pawn, secondPart, out bool deflected, out bool diminished, out bool shieldAbsorbed);
            newDinfo.SetHitPart(secondPart);
            if (newDinfo.Amount > 0)
            {
                ((DamageWorker_AddInjury)instance).FinalizeAndAddInjury(pawn, newDinfo.Amount, newDinfo, result);
            }
        }
    }
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(DamageWorker_Blunt), "ApplySpecialEffectsToPart")]
    static class Patch_DamageWorker_Blunt
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var finalizeAddInjury = AccessTools.Method(typeof(DamageWorker_AddInjury), "FinalizeAndAddInjury", new[]
            {
                typeof(Pawn), typeof(float), typeof(DamageInfo), typeof(DamageWorker.DamageResult)
            });
            var callHelper = AccessTools.Method(typeof(Patch_DamageWorker_Blunt), "ApplyCEBluntDamage");
            var type = typeof(DamageWorker_Blunt).GetNestedType("<>c__DisplayClass1_0", BindingFlags.NonPublic);
            var lastInfo = AccessTools.Field(type, "lastInfo");
            bool foundInjection = false;

            for (int i = 0; i < code.Count - 10; i++)
            {
                if (
                    code[i + 7].Calls(finalizeAddInjury) &&
                    code[i + 8].opcode == OpCodes.Sub &&
                    code[i + 9].opcode == OpCodes.Stloc_3
                )
                {
                    code.RemoveRange(i, 10);
                    var injected = new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Ldloc_3),
                        new CodeInstruction(OpCodes.Ldloc_0),
                        new CodeInstruction(OpCodes.Ldfld, lastInfo),
                        new CodeInstruction(OpCodes.Ldarg_S, 4),
                        new CodeInstruction(OpCodes.Ldarg_3),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Call, callHelper),
                        new CodeInstruction(OpCodes.Sub),
                        new CodeInstruction(OpCodes.Stloc_3),
                    };
                    foundInjection = true;
                    code.InsertRange(i, injected);
                    break;
                }
            }
            if (!foundInjection)
            {
                Log.Error($"Combat Extended :: Failed to find injection point when applying Patch: {HarmonyBase.GetClassName(MethodBase.GetCurrentMethod()?.DeclaringType)}");
            }
            return code;
        }
        public static float ApplyCEBluntDamage(Pawn pawn, float amount, DamageInfo lastInfo, DamageWorker.DamageResult result, DamageInfo originalDinfo, DamageWorker instance)
        {
            if (lastInfo.HitPart == originalDinfo.HitPart)
            {
                return ((DamageWorker_AddInjury)instance).FinalizeAndAddInjury(pawn, amount, lastInfo, result);
            }
            DamageInfo newDinfo = ArmorUtilityCE.GetAfterArmorDamage(lastInfo, pawn, lastInfo.HitPart, out bool deflected, out bool diminished, out bool shieldAbsorbed);
            float injuryAmount = ((DamageWorker_AddInjury)instance).FinalizeAndAddInjury(pawn, newDinfo.Amount, newDinfo, result);
            return Mathf.Max(injuryAmount, lastInfo.Amount);
        }
    }
}

// Should work as long as ShouldReduceDamageToPreservePart exists

[HarmonyPatch(typeof(DamageWorker_AddInjury), nameof(DamageWorker_AddInjury.ShouldReduceDamageToPreservePart))]
static class Patch_ShouldReduceDamageToPreservePart
{
    [HarmonyPrefix]
    static bool Prefix(ref bool __result, BodyPartRecord bodyPart)
    {
        __result = false;
        return false;
    }
}
