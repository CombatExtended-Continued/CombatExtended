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
    public class StatWorker_Caliber : StatWorker
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
            return (req.Thing as Building_TurretGunCE)?.Gun ?? req.Thing;
        }

        public override bool ShouldShowFor(StatRequest req)
        {
            return base.ShouldShowFor(req) && 
                (Controller.settings.EnableAmmoSystem
                    ? GunDef(req)?.GetCompProperties<CompProperties_AmmoUser>()?.ammoSet != null
                    : (GunDef(req)?.Verbs?.Any(x => x.defaultProjectile != null) ?? false));
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (Controller.settings.EnableAmmoSystem)
            {
                var ammoSet = GunDef(req)?.GetCompProperties<CompProperties_AmmoUser>().ammoSet;
                if (ammoSet != null)
                {
                    // Append various ammo stats
                    stringBuilder.AppendLine(ammoSet.LabelCap + "\n");
                    foreach (var cur in ammoSet.ammoTypes)
                    {
                        string label = string.IsNullOrEmpty(cur.ammo.ammoClass.LabelCapShort) ? (string)cur.ammo.ammoClass.LabelCap : cur.ammo.ammoClass.LabelCapShort;
                        stringBuilder.AppendLine(label + ":\n" + cur.projectile.GetProjectileReadout(Gun(req)));   //Is fine handling req.Thing == null, then it sets mult = 1
                    }
                }
            }
            else
            {
                var projectiles = GunDef(req)?.Verbs?.Where(x => x.defaultProjectile != null).Select(x => x.defaultProjectile);

                foreach (var cur in projectiles)
                    stringBuilder.AppendLine(cur.LabelCap + ":\n" + cur.GetProjectileReadout(Gun(req)));
            }

            return stringBuilder.ToString().TrimEndNewlines();
        }

        public override string GetStatDrawEntryLabel(StatDef stat, float value, ToStringNumberSense numberSense, StatRequest optionalReq, bool finalized = true)
        {
            if (Controller.settings.EnableAmmoSystem)
            {
                return GunDef(optionalReq)?.GetCompProperties<CompProperties_AmmoUser>()?.ammoSet?.LabelCap;
            }
            else
            {
                var projectiles = GunDef(optionalReq)?.Verbs?.Where(x => x.defaultProjectile != null).Select(x => x.defaultProjectile);
                return projectiles.First().LabelCap + (projectiles.Count() > 1 ? "(+"+(projectiles.Count() - 1)+" more..)" : "");
            }
        }
    }
}
