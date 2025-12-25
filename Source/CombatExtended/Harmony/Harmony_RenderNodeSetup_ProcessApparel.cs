using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE;

[HarmonyPatch(typeof(DynamicPawnRenderNodeSetup_Apparel), nameof(DynamicPawnRenderNodeSetup_Apparel.ProcessApparel))]
public class Harmony_RenderNodeSetup_ProcessApparel
{
    public static IEnumerable<(PawnRenderNode node, PawnRenderNode parent)> Postfix(IEnumerable<(PawnRenderNode node, PawnRenderNode parent)> input, Apparel ap)
    {
        foreach ((PawnRenderNode node, PawnRenderNode parent) in input)
        {
            if (ap?.def.HasModExtension<ApparelDefExtension>() ?? false)
            {
                var ext = ap.def.GetModExtension<ApparelDefExtension>();
                if (ext.isBackpack)
                {
                    if (node.Props.Worker is PawnRenderNodeWorker_Apparel_Body)
                    {
                        node.Props.subworkerClasses ??= [];
                        node.Props.subworkerClasses.Add(typeof(PawnRenderSubWorker_Backpack));
                    }
                }
                if (ext.isWebbing)
                {
                    if (node.Props.Worker is PawnRenderNodeWorker_Apparel_Body)
                    {
                        node.Props.subworkerClasses ??= [];
                        node.Props.subworkerClasses.Add(typeof(PawnRenderSubWorker_Webbing));
                    }
                }
            }
            yield return (node, parent);
        }
    }
}
