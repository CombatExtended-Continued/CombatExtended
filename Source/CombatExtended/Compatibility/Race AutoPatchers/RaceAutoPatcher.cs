using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace CombatExtended.Compatibility
{
    [StaticConstructorOnStartup]
    public class RaceAutoPatcher
    {
        static RaceAutoPatcher()
        {

            #region Animal patching
            var animalsUnpatched = DefDatabase<ThingDef>.AllDefs.Where(x =>
                   x.race != null &&
                   x.race.Animal &&
                   x.tools.Any(y => !(y is ToolCE))
                );


            foreach (ThingDef animal in animalsUnpatched)
            {
                Log.Message("Patching " +  animal.defName.Colorize(UnityEngine.Color.green));


                var newTools = new List<Tool>();


                #region Tool patching
                foreach (Tool tool in animal.tools)
                {
                    if ( !(tool is ToolCE) )
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

                animal.tools = newTools;

                #endregion

                var RatingSharp = animal.statBases.Find(y => y.stat == StatDefOf.ArmorRating_Sharp);

                if (RatingSharp != null)
                {
                    RatingSharp.value *= 100f;
                }
                else
                {
                    RatingSharp = new StatModifier { stat = StatDefOf.ArmorRating_Sharp, value = 1f };
                    animal.statBases.Add(RatingSharp);
                }

                

                var RatingBlunt = animal.statBases.Find(y => y.stat == StatDefOf.ArmorRating_Blunt);

                if (RatingBlunt != null)
                {
                    RatingBlunt.value *= 100f;
                }
                else
                {
                    RatingBlunt = new StatModifier { stat = StatDefOf.ArmorRating_Blunt, value = 1f };

                    animal.statBases.Add(RatingBlunt);
                }

                
                Log.Message("successfully patched: " + animal.defName.Colorize(UnityEngine.Color.green));
            }

            #endregion

            #region HAR patching if HAR is loaded

            if (ModLister.HasActiveModWithName("Humanoid Alien Races"))
            {
                RaceUtil.PatchHARs();
            }

           

            #endregion


        }
    }
}
