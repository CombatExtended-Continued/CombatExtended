using System.Collections.Generic;
using System.Linq;
using Verse;

namespace CombatExtended
{
    public class CompAmmoListUser : CompVariableAmmoUser
    {
        public new CompProperties_AmmoListUser Props
        {
            get
            {
                return (CompProperties_AmmoListUser)props;
            }
        }
        public override List<AmmoSetDef> UsableAmmoSets
        {
            get
            {
                if (usableAmmoSets.NullOrEmpty())
                {
                    var allowedAmmoSets = new List<AmmoSetDef> { Props.ammoSet };
                    allowedAmmoSets.AddRange(Props.additionalAmmoSets);

                    // Generalize allowed ammosets in generic ammo mode, unless they were already a generic ammoset.
                    if (Controller.settings.GenericAmmo)
                    {
                        foreach (var def in allowedAmmoSets.Select(def => def.similarTo ?? def))
                        {
                            if (IsUsableAmmoSet(def))
                            {
                                usableAmmoSets.Add(def);
                            }
                        }

                        return usableAmmoSets;
                    }

                    foreach (var def in allowedAmmoSets)
                    {
                        if (Props.allowSimilarAmmo)
                        {
                            var similarAmmoSets = DefDatabase<AmmoSetDef>.AllDefs
                                                    .Where(other => other.similarTo == def)
                                                    .ToList();

                            // Allow specifying a generic ammoset directly, and allow its constituent ammosets in this case.
                            if (similarAmmoSets.Any())
                            {
                                foreach (var similarAmmoSet in similarAmmoSets)
                                {
                                    if (IsUsableAmmoSet(similarAmmoSet))
                                    {
                                        usableAmmoSets.Add(similarAmmoSet);
                                    }
                                }

                                continue;
                            }
                        }

                        if (IsUsableAmmoSet(def))
                        {
                            usableAmmoSets.Add(def);
                        }
                    }
                }
                return usableAmmoSets;
            }
        }


        //Merge ammosets with identical ammo usage. I'm worried about its performance and I might want to caculate this during game start up.
        private bool IsUsableAmmoSet(AmmoSetDef def)
        {
            if (def.ammoTypes.First().ammo.menuHidden)
            {
                return false;
            }

            if (usableAmmoSets.NullOrEmpty())
            {
                return true;
            }

            foreach (var v in usableAmmoSets)
            {
                if (IsIdenticalTo(v, def))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsIdenticalTo(AmmoSetDef a, AmmoSetDef b)
        {
            if (a.ammoTypes.Count != b.ammoTypes.Count)
            {
                return false;
            }
            HashSet<AmmoDef> list = new HashSet<AmmoDef>();
            foreach (var v in a.ammoTypes)
            {
                list.Add(v.ammo);
            }
            foreach (var n in b.ammoTypes)
            {
                if (!list.Contains(n.ammo))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
