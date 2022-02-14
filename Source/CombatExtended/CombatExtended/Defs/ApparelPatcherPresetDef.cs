using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CombatExtended
{
    public class ApparelPatcherPresetDef : Def
    {
        #region fields for checking if a def is supposed to be patched

        public List<ApparelLayerDef> neededLayers;

        public FloatRange vanillaArmorRatingRange;

        public List<BodyPartGroupDef> neededGroups;

        #endregion

        #region stats for the preset patched def

        public SimpleCurve ArmorCurveSharp = null;

        public SimpleCurve ArmorCurveBlunt = null;

        public float ArmorStaticSharp;

        public float ArmorStaticBlunt;

        public float Bulk;

        public float Mass;

        public float BulkWorn;

        public List<ApparelPartialStat> partialStats;

        #region methods
        public float FinalRatingSharp(float vanillaRating)
        {
            float result = ArmorStaticSharp;

            if (ArmorCurveSharp != null)
            {
                return ArmorCurveSharp.Evaluate(vanillaRating);
            }

            return result;
        }

        public float FinalRatingBlunt(float vanillaRating)
        {
            float result = ArmorStaticBlunt;

            if (ArmorCurveBlunt != null)
            {
                return ArmorCurveBlunt.Evaluate(vanillaRating);
            }

            return result;
        }
        #endregion

        #endregion
    }
}
