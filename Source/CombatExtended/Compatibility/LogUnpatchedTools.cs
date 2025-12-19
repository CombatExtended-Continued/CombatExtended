using Verse;

namespace CombatExtended.Compatibility;

public static class LogUnpatchedTools
{
    public static void DetectAndLogUnpatchedTools()
    {
        bool logUnpatchedDefsFlag = Controller.settings.LogUnpatchedDefs;
        if (!logUnpatchedDefsFlag)
        {
            return;
        }

        foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
        {
            string unpatchedToolList = "";
            if (def.tools == null)
            {
                continue;
            }
            bool unpatchedTool = false;
            for (int i = 0; i < def.tools.Count; i++)
            {
                Tool tool = def.tools[i];
                if (tool is null or ToolCE)
                {
                    continue;
                }
                unpatchedTool = true;
                unpatchedToolList += tool + "\n";
            }
            if (!unpatchedTool)
            {
                continue;
            }
            Log.Message($"CE: Detected unpatched tool(s) on {def.defName}. Recommend patching or turning on autopatcher. \nUnpatched Tool Capacities:\n {unpatchedToolList}");
        }

    }
}

