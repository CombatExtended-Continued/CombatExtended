using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended.Compatibility
{
    public static class RaceUtil
    {
        public static void PatchHARs()
        {
            Log.Error("Patching alien races".Colorize(Color.magenta));

            var UnpatchedHARs = DefDatabase<ThingDef>.AllDefs.Where(x => x.GetType().ToString() == "AlienRace.ThingDef_AlienRace").Where(x => x.tools.Any(y => !(y is ToolCE) ));

            foreach (ThingDef alienRace in UnpatchedHARs)
            {
                Log.Message("Patching " + alienRace.defName.Colorize(UnityEngine.Color.blue));


                var newTools = new List<Tool>();
                #region Tool patching
                foreach (Tool tool in alienRace.tools)
                {
                    if (!(tool is ToolCE))
                    {
                        ToolCE newTool = new ToolCE();

                        newTool.capacities = tool.capacities;

                        newTool.armorPenetrationSharp = tool.armorPenetration;

                        newTool.armorPenetrationBlunt = tool.armorPenetration;

                        newTool.armorPenetration = tool.armorPenetration;

                        newTool.chanceFactor = tool.chanceFactor;

                        newTool.power = tool.power;

                        newTool.linkedBodyPartsGroup = tool.linkedBodyPartsGroup;

                        newTool.label = tool.label;

                        newTools.Add(newTool);
                    }
                }

                alienRace.tools = newTools;
                #endregion

                if (alienRace.comps == null)
                {
                    alienRace.comps = new List<CompProperties>();
                }

                alienRace.comps.Add(new CompProperties_Inventory());


                alienRace.comps.Add(new CompProperties_Suppressable());

                alienRace.comps.Add(new CompProperties { compClass = typeof(CompPawnGizmo) });

                Log.Message("Succesfully patched: " + alienRace.defName.Colorize(UnityEngine.Color.blue));
            }
        }
    }
}
