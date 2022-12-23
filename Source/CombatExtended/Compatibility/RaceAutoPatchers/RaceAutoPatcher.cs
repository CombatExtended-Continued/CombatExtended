﻿using System;
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
            if (!Controller.settings.EnableRaceAutopatcher)
            {
                return;
            }
            #region Animal patching
            var animalsUnpatched = DefDatabase<ThingDef>.AllDefs.Where(x =>
                                   x.race != null &&
                                   x.race.Animal &&
                                   x.tools != null &&
                                   x.tools.Any(y => y != null && !(y is ToolCE))
                                                                      );

            int patchCount = 0;
            string patchedanimals = "";
            foreach (ThingDef animal in animalsUnpatched)
            {
                if (animal.modExtensions == null)
                {
                    animal.modExtensions = new List<DefModExtension>();
                }

                animal.modExtensions.Add(new RacePropertiesExtensionCE { bodyShape = CE_BodyShapeDefOf.Quadruped });

                var newTools = new List<Tool>();


                #region Tool patching
                foreach (Tool tool in animal.tools)
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

                animal.tools = newTools;

                #endregion

                var RatingSharp = animal.statBases.Find(y => y.stat == StatDefOf.ArmorRating_Sharp);

                if (RatingSharp != null)
                {
                    RatingSharp.value = RaceUtil.SharpCurve.Evaluate(RatingSharp.value);

                    var RatingSharpBP = new StatModifier { stat = CE_StatDefOf.BodyPartSharpArmor, value = RatingSharp.value };

                    animal.statBases.Add(RatingSharpBP);
                }
                else
                {
                    RatingSharp = new StatModifier { stat = StatDefOf.ArmorRating_Sharp, value = 0.125f };
                    animal.statBases.Add(RatingSharp);

                    var RatingSharpBP = new StatModifier { stat = CE_StatDefOf.BodyPartSharpArmor, value = 1f };

                    animal.statBases.Add(RatingSharpBP);
                }



                var RatingBlunt = animal.statBases.Find(y => y.stat == StatDefOf.ArmorRating_Blunt);

                if (RatingBlunt != null)
                {
                    RatingBlunt.value = RaceUtil.BluntCurve.Evaluate(RatingBlunt.value);

                    var RatingBluntBP = new StatModifier { stat = CE_StatDefOf.BodyPartBluntArmor, value = RatingBlunt.value };

                    animal.statBases.Add(RatingBluntBP);
                }
                else
                {
                    RatingBlunt = new StatModifier { stat = StatDefOf.ArmorRating_Blunt, value = 1f };

                    animal.statBases.Add(RatingBlunt);

                    var RatingBluntBP = new StatModifier { stat = CE_StatDefOf.BodyPartBluntArmor, value = 1f };

                    animal.statBases.Add(RatingBluntBP);
                }

                patchCount++;
                patchedanimals += animal.ToString() + "\n";
            }
            // Don't show log if there are no animals being patched by autopatcher.
            if (patchCount > 0)
            {
                Log.Message("CE successfully patched " + patchCount.ToString() + " animals.\nAnimal Defs autopatched:" + patchedanimals);
            }
            //Log.Message(patchedanimals);

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
