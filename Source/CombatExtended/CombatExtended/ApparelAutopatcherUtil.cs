using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace CombatExtended
{
    public static class ApparelAutopatcherUtil
    {
        public static void AddModExtension(this ThingDef def, DefModExtension value)
        {
            if (def.modExtensions == null)
            {
                def.modExtensions = new List<DefModExtension>();
            }

            def.modExtensions.Add(value);
        }
        public static float GetStatValueDef(this ThingDef def, StatDef statDef)
        {
            return def.statBases?.Find(x => x.stat == statDef)?.value ?? 0f;
        }

        //must be used when null apparel defs are already filtered out
        public static bool Matches(this ThingDef matchee, ApparelPatcherPresetDef matcher)
        {
            bool result = false;

            result =
                matchee.apparel.layers.All(x => matcher.neededLayers.Contains(x))
                && matchee.apparel.bodyPartGroups.All(x => matcher.neededGroups.Contains(x))
                && 
                (
                matcher.vanillaArmorRatingRange.Includes(matchee.GetStatValueDef(StatDefOf.ArmorRating_Sharp))
                |
                matcher.vanillaArmorRatingRange.Includes(matchee.GetStatValueDef(StatDefOf.StuffEffectMultiplierArmor))
                )
                ;

            return result;
        }
    }
}
