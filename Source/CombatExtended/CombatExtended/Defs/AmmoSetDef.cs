using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class AmmoSetDef : Def
    {
        public List<AmmoLink> ammoTypes;
        // mortar ammo should still availabe when the ammo system is off
        public bool isMortarAmmoSet = false;

        public AmmoSetDef similarTo;

        public int ammoConsumedPerShot = 1;

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
        {
            foreach (StatDrawEntry entry in base.SpecialDisplayStats(req)) { yield return entry; }

            foreach (AmmoLink link in ammoTypes)
            {

                yield return new StatDrawEntry(StatCategoryDefOf.BasicsImportant, link.ammo.label, "", link.projectile.GetProjectileReadout(null), 1, hyperlinks: new List<Dialog_InfoCard.Hyperlink>() { new Dialog_InfoCard.Hyperlink(link.ammo) });
            }
        }
    }
}
