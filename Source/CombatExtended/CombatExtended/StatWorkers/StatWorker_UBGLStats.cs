using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace CombatExtended
{
    public class StatWorker_UBGLStats : StatWorker
    {
        public override bool ShouldShowFor(StatRequest req)
        {
            var comp = req.Thing?.TryGetComp<CompUnderBarrel>()?.Props ?? null;
            if (comp == null && req.Def != null)
                comp = (req.Def as ThingDef)?.GetCompProperties<CompProperties_UnderBarrel>() ?? null;
            return base.ShouldShowFor(req) && comp != null;
        }
    
        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            var comp = req.Thing?.TryGetComp<CompUnderBarrel>()?.Props ?? null;
            if (comp == null)
                comp = ((ThingDef)req.Def)?.GetCompProperties<CompProperties_UnderBarrel>() ?? null;
            if (comp != null)
            {
                var builder = new StringBuilder();

                builder.AppendLine("WarmupTime".Translate() + ": " + comp.verbPropsUnderBarrel.warmupTime);
                builder.AppendLine("Range".Translate() + ": " + comp.verbPropsUnderBarrel.range);
                builder.AppendLine("CE_AmmoSet".Translate() + ": " + comp.propsUnderBarrel.ammoSet);
                builder.AppendLine("CE_MagazineSize".Translate() + ": " + comp.propsUnderBarrel.magazineSize);
                return builder.ToString();
            }
            return base.GetExplanationUnfinalized(req, numberSense);
        }

        public override string ValueToString(float val, bool finalized, ToStringNumberSense numberSense = ToStringNumberSense.Absolute)
        {
            return "CE_UBGLStats_Title".Translate();
        }
    }
}
