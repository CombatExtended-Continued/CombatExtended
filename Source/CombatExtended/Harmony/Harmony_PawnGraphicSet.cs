using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace CombatExtended.HarmonyCE;
//[HarmonyPatch(typeof(PawnGraphicSet), "MatsBodyBaseAt")]
internal static class Harmony_PawnGraphicSet
{
    private static bool RenderSpecial(ApparelLayerDef layer)
    {
        return (layer.GetModExtension<ApparelLayerExtension>()?.IsHeadwear ?? false) || layer.drawOrder > ApparelLayerDefOf.Shell.drawOrder;
    }

    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var write = false;
        bool foundInjection = false;

        foreach (var code in instructions)
        {
            if (write)
            {
                write = false;
                code.opcode = OpCodes.Brtrue;
            }

            if (code.opcode == OpCodes.Ldsfld && ReferenceEquals(code.operand, AccessTools.Field(typeof(ApparelLayerDefOf), nameof(ApparelLayerDefOf.Overhead))))
            {
                write = true;
                foundInjection = true;
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Harmony_PawnGraphicSet), nameof(RenderSpecial)));
            }
            else
            {
                yield return code;
            }
        }
        if (!foundInjection)
        {
            Log.Error($"Combat Extended :: Failed to find injection point when applying Patch: {HarmonyBase.GetClassName(MethodBase.GetCurrentMethod()?.DeclaringType)}");
        }
    }
}
