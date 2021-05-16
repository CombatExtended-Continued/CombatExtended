﻿using System.Text;
using RimWorld;
using Verse;

namespace CombatExtended
{
    public class StatWorker_ArmorCoverageSharp : StatWorker
    {
        public override bool ShouldShowFor(StatRequest req)
        {
            return req.HasThing && (req.Thing as Pawn)?.apparel != null;
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            var weightedArmor = 0f;
            var totalCoverage = 0f;

            var pawn = (Pawn)req.Thing;
            foreach (var apparel in pawn.apparel.WornApparel)
            {
                var coverage = apparel.def.apparel.HumanBodyCoverage;
                weightedArmor += apparel.GetStatValue(StatDefOf.ArmorRating_Sharp) * coverage;
                totalCoverage += coverage;
            }

            return totalCoverage > 0 ? weightedArmor /*/ totalCoverage*/ : 0; // Why? I'd be more scared to be dressed only in power armor than with power armor and a helmet
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            var stringBuilder = new StringBuilder(base.GetExplanationUnfinalized(req, numberSense));

            var pawn = (Pawn)req.Thing;
            if (pawn.apparel.WornApparelCount > 0)
            {
                stringBuilder.AppendLine();
                foreach (var apparel in pawn.apparel.WornApparel)
                {
                    stringBuilder.AppendLine($"{apparel.LabelCap}: {apparel.GetStatValue(StatDefOf.ArmorRating_Sharp)} x {apparel.def.apparel.HumanBodyCoverage.ToStringPercent()}");
                }

                stringBuilder.AppendLine();
            }

            return stringBuilder.ToString().Trim();
        }
    }
}