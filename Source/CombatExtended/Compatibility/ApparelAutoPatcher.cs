using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace CombatExtended
{
    [StaticConstructorOnStartup]
    public class ApparelAutoPatcher
    {
        static ApparelAutoPatcher()
        {
            var Apparels = DefDatabase<ThingDef>.AllDefs.Where(x => x.IsApparel && !x.statBases.Any(x => x.stat == CE_StatDefOf.Bulk) && !x.statBases.Any(x => x.stat == CE_StatDefOf.WornBulk));

            foreach (var preset in DefDatabase<ApparelPatcherPresetDef>.AllDefs)
            {
                var toPatch = Apparels.Where(x => x.Matches(preset));

                foreach (var apparel in toPatch)
                {
                    Log.Message("Autopatching " + apparel.label);

                    if (apparel.statBases == null)
                    {
                        apparel.statBases = new List<StatModifier>();
                    }

                    apparel.statBases.Add(new StatModifier { stat = CE_StatDefOf.Bulk, value = preset.Bulk });

                    apparel.statBases.Add(new StatModifier { stat = CE_StatDefOf.WornBulk, value = preset.BulkWorn });

                    StatModifier[] ArmorRatings = new StatModifier[]
                    {
                       new StatModifier { value = preset.FinalRatingSharp(apparel.GetStatValueDef(StatDefOf.ArmorRating_Sharp)), stat = StatDefOf.ArmorRating_Sharp },
                       new StatModifier { value = preset.FinalRatingBlunt(apparel.GetStatValueDef(StatDefOf.ArmorRating_Blunt)), stat = StatDefOf.ArmorRating_Blunt }
                    };

                    apparel.statBases.RemoveAll(x => 
                    x.stat == StatDefOf.ArmorRating_Sharp 
                    |
                    x.stat == StatDefOf.ArmorRating_Blunt
                    |
                    x.stat == StatDefOf.StuffEffectMultiplierArmor
                    );
                    apparel.statBases.AddRange(ArmorRatings);

                    var mass = apparel.statBases.Find(x => x.stat == StatDefOf.Mass);

                    if (mass != null)
                    {
                        mass.value = preset.Mass;
                    }
                    else
                    {
                        apparel.statBases.Add(new StatModifier { stat = StatDefOf.Mass, value = preset.Mass });
                    }

                    if (preset.partialStats != null)
                    {
                        apparel.AddModExtension(new PartialArmorExt { stats = preset.partialStats.ListFullCopy() });
                    }

                    Log.Message("AutoPatched " + apparel.label + " as " + preset.label);
                }
            }
        }
    }
}
