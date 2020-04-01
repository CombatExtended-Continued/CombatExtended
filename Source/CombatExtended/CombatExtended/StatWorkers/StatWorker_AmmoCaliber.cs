using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using HarmonyLib;

namespace CombatExtended
{
    public class StatWorker_AmmoCaliber : StatWorker
    {
        public override bool ShouldShowFor(StatRequest req)
        {
            return base.ShouldShowFor(req) && (!(req.Def as AmmoDef)?.Users.NullOrEmpty() ?? false);
        }

        public override IEnumerable<Dialog_InfoCard.Hyperlink> GetInfoCardHyperlinks(StatRequest statRequest)
        {
            var ammoDef = (statRequest.Def as AmmoDef);

            if (ammoDef != null)
            {
                var users = ammoDef.Users;

                if (!users.NullOrEmpty())
                {
                    foreach (var user in users)
                    {
                        yield return new Dialog_InfoCard.Hyperlink(user);
                    }
                }
            }
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            StringBuilder stringBuilder = new StringBuilder();
            var ammoDef = (req.Def as AmmoDef);

            if (ammoDef != null)
            {
                var users = ammoDef.Users;

                if (!users.NullOrEmpty())
                {
                    var ammoSetDefs = ammoDef.AmmoSetDefs;
                    var count = ammoSetDefs.Count;

                    foreach (var ammoSet in ammoSetDefs)
                    {
                        var launcherNameArray = users.Where(x => count == 1 || x.GetCompProperties<CompProperties_AmmoUser>()?.ammoSet == ammoSet)
                                            .Select(y => y.label.CapitalizeFirst())
                                            .ToArray();

                        var projectile = ammoSet.ammoTypes.Find(x => x.ammo == (req.Def as AmmoDef)).projectile;

                        stringBuilder.AppendLine(ammoSet.LabelCap + " (" + string.Join(", ", launcherNameArray) + "):\n"
                            + projectile.GetProjectileReadout(null));   //Is fine handling req.Thing == null, then it sets mult = 1
                    }
                }
            }

            return stringBuilder.ToString().TrimEndNewlines();
        }

        public override string GetStatDrawEntryLabel(StatDef stat, float value, ToStringNumberSense numberSense, StatRequest optionalReq, bool finalized = true)
        {
            var list = (optionalReq.Def as AmmoDef)?.AmmoSetDefs;
            return list.FirstOrDefault().LabelCap + (list.Count > 1 ? " (+"+(list.Count - 1)+")" : "");
        }
    }
}
