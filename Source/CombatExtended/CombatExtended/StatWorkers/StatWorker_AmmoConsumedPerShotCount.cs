using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using CombatExtended.Compatibility;


namespace CombatExtended
{
    public class StatWorker_AmmoConsumedPerShotCount : StatWorker
    {
        private ThingDef GunDef(StatRequest req)
        {
            var def = req.Def as ThingDef;

            if (def?.building?.IsTurret ?? false)
                def = def.building.turretGunDef;

            return def;
        }

        private Thing Gun(StatRequest req)
        {
            return (req.Thing as Building_Turret)?.GetGun() ?? req.Thing;
        }

        public override bool ShouldShowFor(StatRequest req)
        {
            return base.ShouldShowFor(req) && (GunDef(req)?.Verbs?
                .Any(x => ((x as VerbPropertiesCE)?.ammoConsumedPerShotCount ?? 1) > 1) ?? false);
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            return ((VerbPropertiesCE)GunDef(req)?.Verbs?.FirstOrDefault(x => ((VerbPropertiesCE)x).ammoConsumedPerShotCount > 1))?.ammoConsumedPerShotCount ?? 1;
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("");
            return stringBuilder.ToString().TrimEndNewlines();
        }
    }
}
