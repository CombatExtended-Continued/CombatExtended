using System.Linq;
using Verse;

namespace CombatExtended
{
    public class DamageWorker_BluntCE : DamageWorker_AddInjury
    {
        public override BodyPartRecord ChooseHitPart(DamageInfo dinfo, Pawn pawn)
        {
            var parts = pawn.health.hediffSet.GetNotMissingParts(dinfo.Height, dinfo.Depth);

            if (!parts.Any())
            {
                parts = pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, dinfo.Depth);
                if (!parts.Any())
                {
                    parts= pawn.health.hediffSet.GetNotMissingParts();
                }
            }

            parts.Where(p => p.depth == BodyPartDepth.Outside || p.def.IsSolid(p, pawn.health.hediffSet.hediffs))
                .TryRandomElementByWeight(p => p.coverageAbs * p.def.GetHitChanceFactorFor(dinfo.Def), out var result);

            return result;
        }
    }
}