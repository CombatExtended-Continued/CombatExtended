using System.Collections.Generic;
using System.Linq;
using Verse;

namespace CombatExtended
{
    public class CompAmmoListUserGeneric : CompAmmoUserGeneric
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
                List<AmmoSetDef> tempGenericAmmoSet = new List<AmmoSetDef>
                {
                    Props.ammoSet.similarTo ?? Props.ammoSet
                };
                foreach (var addAmmo in Props.additionalAmmoSets)
                {
                    AmmoSetDef tempAmmoSet = addAmmo.similarTo ?? addAmmo;
                    if (!tempGenericAmmoSet.Contains(tempAmmoSet))
                    {
                        tempGenericAmmoSet.Add(tempAmmoSet);
                    }
                }

                if (usableAmmoSets.NullOrEmpty())
                {
                    foreach (var def in DefDatabase<AmmoSetDef>.AllDefs)
                    {
                        if (def.similarTo == null)
                        {
                            continue;
                        }
                        if (tempGenericAmmoSet.Contains(def.similarTo) && !def.ammoTypes.First().ammo.menuHidden && !IsIdenticalToAny(def))
                        {
                            usableAmmoSets.Add(def);
                        }
                    }
                }
                return usableAmmoSets;
            }
        }
    }
}
