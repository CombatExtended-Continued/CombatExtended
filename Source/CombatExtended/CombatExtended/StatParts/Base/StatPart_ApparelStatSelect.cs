using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
using static CombatExtended.StatPart_ApparelStatMaxima;

namespace CombatExtended
{
    public abstract class StatPart_ApparelStatSelect : StatPart
    {
        public StatDef apparelStat;

        private Dictionary<ApparelStatKey, float> cachedStats = new Dictionary<ApparelStatKey, float>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetApparelEfficiency(Apparel apparel)
        {
            ApparelStatKey key = new ApparelStatKey(apparel);

            return cachedStats.TryGetValue(key, out float value) ?
                value : cachedStats[key] = apparel.GetStatValue(apparelStat);
        }

        protected abstract float Select(float first, float second);

        public override void TransformValue(StatRequest req, ref float result)
        {
            if (req.HasThing && req.Thing is Pawn pawn && pawn.apparel != null)
            {
                ThingOwner<Apparel> wornApparel = pawn.apparel.wornApparel;

                for (int i = 0; i < wornApparel.Count; i++)
                    result = Select(result, GetApparelEfficiency(wornApparel[i]));
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (req.HasThing && req.Pawn != null)
            {
                return "";
            }
            return null;
        }
    }
}
