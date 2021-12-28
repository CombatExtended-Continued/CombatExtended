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
        public static ToolCE ConvertTool(this Tool tool)
        {
            var newTool = new ToolCE()
            {
                capacities = tool.capacities,
                armorPenetrationSharp = tool.armorPenetration,
                armorPenetrationBlunt = tool.armorPenetration,
                cooldownTime = tool.cooldownTime,
                chanceFactor = tool.chanceFactor,
                power = tool.power,
                label = tool.label,
                linkedBodyPartsGroup = tool.linkedBodyPartsGroup
            };

            if (newTool.cooldownTime <= 0)
            {
                newTool.cooldownTime = 2f;
            }

            if (newTool.armorPenetrationSharp <= 0)
            {
                newTool.armorPenetrationSharp = 0.5f;
                newTool.armorPenetrationBlunt = 2f;
            }

            return newTool;
        }
        public static void PatchHARs()
        {

            var UnpatchedHARs = DefDatabase<ThingDef>.AllDefs.Where(x => x.GetType().ToString() == "AlienRace.ThingDef_AlienRace").Where(x => x.tools.Any(y => !(y is ToolCE) ));

            int patchCount = 0;

            foreach (ThingDef alienRace in UnpatchedHARs)
            {

                if (alienRace.modExtensions == null)
                {
                    alienRace.modExtensions = new List<DefModExtension>();
                }

                alienRace.modExtensions.Add(new RacePropertiesExtensionCE { bodyShape = CE_BodyShapeDefOf.Humanoid });

                var newTools = new List<Tool>();
                #region Tool patching
                foreach (Tool tool in alienRace.tools)
                {
                    if (!(tool is ToolCE))
                    {
                        newTools.Add(tool.ConvertTool());
                    }
                    else
                    {
                        newTools.Add(tool);
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

                patchCount++;
            }

            Log.Message("CE successfully patched " + patchCount.ToString() + " humanoid alien races");
        }
    }
}
