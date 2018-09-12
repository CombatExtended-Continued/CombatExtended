using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class StatWorker_Magazine : StatWorker
    {
        public override bool ShouldShowFor(StatRequest req)
        {
            return base.ShouldShowFor(req) && req.Thing?.TryGetComp<CompAmmoUser>()?.Props.magazineSize > 0;
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            return req.Thing?.TryGetComp<CompAmmoUser>().Props.magazineSize ?? 0;
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            StringBuilder stringBuilder = new StringBuilder();
            var ammoProps = (req.Def as ThingDef)?.GetCompProperties<CompProperties_AmmoUser>();
            stringBuilder.AppendLine("CE_MagazineSize".Translate() + ": " + GenText.ToStringByStyle(ammoProps.magazineSize, ToStringStyle.Integer));
            stringBuilder.AppendLine("CE_ReloadTime".Translate() + ": " + GenText.ToStringByStyle((ammoProps.reloadTime), ToStringStyle.FloatTwo) + " s");
            return stringBuilder.ToString().TrimEndNewlines();
        }

        public override string GetStatDrawEntryLabel(StatDef stat, float value, ToStringNumberSense numberSense, StatRequest optionalReq)
        {
            var ammoProps = (optionalReq.Def as ThingDef)?.GetCompProperties<CompProperties_AmmoUser>();
            return ammoProps.magazineSize + " / " + GenText.ToStringByStyle((ammoProps.reloadTime), ToStringStyle.FloatTwo) + " s";
        }
    }
}
