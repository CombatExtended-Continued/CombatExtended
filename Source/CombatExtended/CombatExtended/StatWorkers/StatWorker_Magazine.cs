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
        public override bool ShouldShowFor(BuildableDef eDef)
        {
            var thingDef = eDef as ThingDef;
            return thingDef?.GetCompProperties<CompProperties_AmmoUser>()?.magazineSize > 0;
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            return req.Thing.TryGetComp<CompAmmoUser>().Props.magazineSize;
        }

        public override string GetExplanation(StatRequest req, ToStringNumberSense numberSense)
        {
            StringBuilder stringBuilder = new StringBuilder();
            var ammoComp = req.Thing.TryGetComp<CompAmmoUser>();
            stringBuilder.AppendLine("CE_MagazineSize".Translate() + ": " + GenText.ToStringByStyle(ammoComp.Props.magazineSize, ToStringStyle.Integer));
            stringBuilder.AppendLine("CE_ReloadTime".Translate() + ": " + GenText.ToStringByStyle((ammoComp.Props.reloadTime), ToStringStyle.FloatTwo) + " s");
            return stringBuilder.ToString().TrimEndNewlines();
        }

        public override string GetStatDrawEntryLabel(StatDef stat, float value, ToStringNumberSense numberSense, StatRequest optionalReq)
        {
            var ammoComp = optionalReq.Thing.TryGetComp<CompAmmoUser>();
            return ammoComp.Props.magazineSize + " / " + GenText.ToStringByStyle((ammoComp.Props.reloadTime), ToStringStyle.FloatTwo) + " s";
        }

        public override void FinalizeExplanation(StringBuilder sb, StatRequest req, ToStringNumberSense numberSense, float finalVal)
        {
        }
    }
}
