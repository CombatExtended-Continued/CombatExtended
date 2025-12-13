using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE;

[HarmonyPatch(typeof(Building_OutfitStand), "RecacheGraphics")]
internal static class Harmony_Building_OutfitStand
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsHeadwear(ApparelLayerDef layer)
    {
        return layer.GetModExtension<ApparelLayerExtension>()?.IsHeadwear ?? false;
    }

    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = instructions.ToList();
        MethodInfo method = AccessTools.Method(typeof(Harmony_Building_OutfitStand), nameof(Harmony_Building_OutfitStand.IsHeadwear));
        bool foundInjection = false;

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Stloc_S && codes[i].operand is LocalBuilder local && local.LocalIndex == 4)
            {
                codes.InsertRange(i + 1, [
                    new CodeInstruction(OpCodes.Ldloc_3),
                    new CodeInstruction(OpCodes.Call, method),
                    new CodeInstruction(OpCodes.Ldloc_S, local),
                    new CodeInstruction(OpCodes.Or),             // bitwise Or so if either is true it will return true
                    new CodeInstruction(OpCodes.Stloc_S, local)
                ]);
                foundInjection = true;

                break;
            }
        }
        if (!foundInjection)
        {
            Log.Error($"Combat Extended :: Failed to find injection point when applying Patch: {HarmonyBase.GetClassName(MethodBase.GetCurrentMethod()?.DeclaringType)}");
        }
        return codes;

    }
}
