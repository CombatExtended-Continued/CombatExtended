using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;
namespace CombatExtended
{
    public class StatWorker_Magazine : StatWorker
    {
        private ThingDef GunDef(StatRequest req)
        {
            var def = req.Def as ThingDef;

            if (def?.building?.IsTurret ?? false)
                def = def.building.turretGunDef;

            return def;
        }

        public override bool ShouldShowFor(StatRequest req)
        {
            return base.ShouldShowFor(req) && (GunDef(req)?.GetCompProperties<CompProperties_AmmoUser>()?.magazineSize ?? 0) > 0;
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            return GunDef(req)?.GetCompProperties<CompProperties_AmmoUser>()?.magazineSize ?? 0;
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            StringBuilder stringBuilder = new StringBuilder();
            var ammoProps = GunDef(req)?.GetCompProperties<CompProperties_AmmoUser>();
            stringBuilder.AppendLine("CE_MagazineSize".Translate() + ": " + GenText.ToStringByStyle(ammoProps.magazineSize, ToStringStyle.Integer));
            stringBuilder.AppendLine("CE_ReloadTime".Translate() + ": " + GenText.ToStringByStyle((ammoProps.reloadTime), ToStringStyle.FloatTwo) + " " + "LetterSecond".Translate());
            return stringBuilder.ToString().TrimEndNewlines();
        }

        public override string GetStatDrawEntryLabel(StatDef stat, float value, ToStringNumberSense numberSense, StatRequest optionalReq, bool finalized = true)
        {
            var ammoProps = GunDef(optionalReq)?.GetCompProperties<CompProperties_AmmoUser>();
            return ammoProps.magazineSize + " / " + GenText.ToStringByStyle((ammoProps.reloadTime), ToStringStyle.FloatTwo) + " "+"LetterSecond".Translate();
        }
    }
}
