using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended;
[HarmonyPatch(typeof(ResearchPrerequisitesUtility), nameof(ResearchPrerequisitesUtility.UnlockedDefsGroupedByPrerequisites))]
public class Harmony_ResearchPrerequisitesUtility
{
    public static List<Pair<ResearchPrerequisitesUtility.UnlockedHeader, List<Def>>> Postfix(List<Pair<ResearchPrerequisitesUtility.UnlockedHeader, List<Def>>> input)
    {
        foreach (var pair in input)
        {
            pair.second.RemoveWhere(x => x is AmmoDef ammoDef && ammoDef.menuHidden);
        }
        input.RemoveWhere(x => x.second.NullOrEmpty());
        return input;
    }
}
