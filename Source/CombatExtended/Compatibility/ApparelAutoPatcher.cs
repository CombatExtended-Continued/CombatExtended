using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace CombatExtended;
[StaticConstructorOnStartup]
public class ApparelAutoPatcher
{
    static ApparelAutoPatcher()
    {
        if (!Controller.settings.EnableApparelAutopatcher)
        {
            return;
        }
        HashSet<ThingDef> patched = new HashSet<ThingDef>();
        var blacklist = new HashSet<string>
        (from a in DefDatabase<ApparelModBlacklist>.AllDefs
         from b in a.modIDs
         select b.ToLower());

        var blacklistDefNames = new HashSet<string>
        (from a in DefDatabase<ApparelModBlacklist>.AllDefs
         from b in a.defNames
         select b);


        HashSet<ModContentPack> mods = new HashSet<ModContentPack>
        (LoadedModManager.RunningMods.Where
         (x => !blacklist.Contains(x.PackageId)));


        var Apparels = DefDatabase<ThingDef>.AllDefs.Where
                       (x => x.IsApparel &&
                        mods.Contains(x.modContentPack) &&
                        !blacklistDefNames.Contains(x.defName) &&
                        !x.statBases.Any(x => x.stat == CE_StatDefOf.Bulk) &&
                        !x.statBases.Any(x => x.stat == CE_StatDefOf.WornBulk));

        if (Controller.settings.DebugAutopatcherLogger)
        {
            foreach (var apparel in Apparels)
            {
                Log.Message($"Seeking patches for {apparel} from {apparel.modContentPack.PackageId}");
            }
        }

        foreach (var preset in DefDatabase<ApparelPatcherPresetDef>.AllDefs)
        {
            var toPatch = Apparels.Where(x => x.Matches(preset) && !patched.Contains(x));

            foreach (var apparel in toPatch)
            {
                patched.Add(apparel);
                if (Controller.settings.DebugAutopatcherLogger)
                {
                    Log.Message($"Autopatching {apparel.label} from {apparel.modContentPack.PackageId}");
                }

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

                if (!apparel.statBases.Any(x => x.stat == StatDefOf.ArmorRating_Sharp))
                {
                    ArmorRatings = new StatModifier[]
                    {
                        new StatModifier { value = preset.FinalRatingSharp(apparel.GetStatValueDef(StatDefOf.StuffEffectMultiplierArmor)), stat = StatDefOf.ArmorRating_Sharp },
                        new StatModifier { value = preset.FinalRatingBlunt(apparel.GetStatValueDef(StatDefOf.StuffEffectMultiplierArmor)), stat = StatDefOf.ArmorRating_Blunt }
                    };

                }



                apparel.statBases.RemoveAll(x =>
                                            x.stat == StatDefOf.ArmorRating_Sharp
                                            ||
                                            x.stat == StatDefOf.ArmorRating_Blunt
                                            ||
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

                Log.Message("AutoPatched " + apparel.label + "(" + apparel.defName + ")" + " as " + preset.label);
            }
        }
    }
}
