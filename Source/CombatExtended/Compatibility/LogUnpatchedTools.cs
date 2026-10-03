using System.Text;
using Verse;

namespace CombatExtended.Compatibility;

public static class UnpatchedDefChecker
{
    public static void DetectAndLogUnpatchedDefs()
    {
        if (!Controller.settings.LogUnpatchedDefs)
        {
            return;
        }
        foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
        {
            CheckUnpatchedTools(def);
            CheckUnpatchedBodyShape(def);
        }

        foreach (HediffDef hediff in DefDatabase<HediffDef>.AllDefs)
        {
            CheckUnpatchedTools(hediff);
        }
    }

    private static void CheckUnpatchedTools(ThingDef def)
    {
        if (def.tools.NullOrEmpty())
        {
            return;
        }

        StringBuilder unpatchedTools = null;

        for (int i = 0; i < def.tools.Count; i++)
        {
            Tool tool = def.tools[i];

            if (tool is null or ToolCE)
            {
                continue;
            }

            unpatchedTools ??= new StringBuilder();
            unpatchedTools.AppendLine(tool.ToString());
        }

        if (unpatchedTools == null)
        {
            return;
        }

        Log.Message($"CE: Detected unpatched tool(s) on {def.defName}. Recommend patching or turning on autopatcher. \nUnpatched Tool Capacities:\n {unpatchedTools}");
    }

    private static void CheckUnpatchedTools(HediffDef hediff)
    {
        if (hediff.comps.NullOrEmpty())
        {
            return;
        }

        for (int i = 0; i < hediff.comps.Count; i++)
        {
            if (hediff.comps[i] is not HediffCompProperties_VerbGiver verbGiver ||
                verbGiver.tools.NullOrEmpty())
            {
                continue;
            }

            StringBuilder unpatchedTools = null;

            for (int j = 0; j < verbGiver.tools.Count; j++)
            {
                Tool tool = verbGiver.tools[j];

                if (tool is null or ToolCE)
                {
                    continue;
                }

                unpatchedTools ??= new StringBuilder();
                unpatchedTools.AppendLine(tool.ToString());
            }

            if (unpatchedTools == null)
            {
                continue;
            }

            Log.Message($"CE: Detected unpatched tool(s) on {hediff}. Recommend patching or turning on autopatcher. \nUnpatched Tool Capacities:\n {unpatchedTools}");
        }
    }

    private static void CheckUnpatchedBodyShape(ThingDef def)
    {
        if (def.race == null || def.GetModExtension<RacePropertiesExtensionCE>() != null)
        {
            return;
        }
        Log.Message($"CE: Detected missing bodyshape on {def.race}. Recommend patching or turning on autopatcher.");
    }

}
