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
            if ((req.Thing?.TryGetComp<CompAmmoUser>()?.Props?.ammoSet ?? null) != (((CompProperties_AmmoUser)req.Thing?.def?.comps?.Find(x => x is CompProperties_AmmoUser))?.ammoSet ?? null))
                return req.Thing.TryGetComp<CompAmmoUser>().Props.magazineSize;
            float size = GunDef(req)?.GetCompProperties<CompProperties_AmmoUser>()?.magazineSize ?? 0;
            return size;
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {            
            StringBuilder stringBuilder = new StringBuilder();
            //var ammoProps = GunDef(req)?.GetCompProperties<CompProperties_AmmoUser>();
            stringBuilder.AppendLine("CE_MagazineSize".Translate() + ": " + GenText.ToStringByStyle(GetMagSize(req), ToStringStyle.Integer));
            //stringBuilder.AppendLine("CE_ReloadTime".Translate() + ": " + GenText.ToStringByStyle((ammoProps.reloadTime), ToStringStyle.FloatTwo) + " " + "LetterSecond".Translate());
            return stringBuilder.ToString().TrimEndNewlines();
        }

        public override string GetStatDrawEntryLabel(StatDef stat, float value, ToStringNumberSense numberSense, StatRequest optionalReq, bool finalized = true)
        {
            if (!optionalReq.HasThing)
            {
                var ammoProps = GunDef(optionalReq)?.GetCompProperties<CompProperties_AmmoUser>();
                return ammoProps.magazineSize.ToString();
                //+ " / " + GenText.ToStringByStyle((ammoProps.reloadTime), ToStringStyle.FloatTwo) + " " + "LetterSecond".Translate();
            }
            else
            {
                //var ammoProps = GunDef(optionalReq)?.GetCompProperties<CompProperties_AmmoUser>();
                return GetMagSize(optionalReq).ToString();
                //" / " + GenText.ToStringByStyle((ammoProps.reloadTime), ToStringStyle.FloatTwo) + " " + "LetterSecond".Translate();
            }
        }

        private int GetMagSize(StatRequest req)
        {
            if ((req.Thing?.TryGetComp<CompAmmoUser>()?.Props?.ammoSet ?? null) != (((CompProperties_AmmoUser)req.Thing?.def?.comps?.Find(x => x is CompProperties_AmmoUser))?.ammoSet ?? null))
                return req.Thing.TryGetComp<CompAmmoUser>().Props.magazineSize;
            if (req.HasThing)
                return (int)req.Thing.GetStatValue(CE_StatDefOf.MagazineCapacity);
            return GunDef(req)?.GetCompProperties<CompProperties_AmmoUser>()?.magazineSize ?? 0;
        }
    }
}
