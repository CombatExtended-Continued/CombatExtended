using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Verse;

namespace CombatExtended
{

    public class CompAmmoUserGeneric : CompVariableAmmoUser
    {
        public AmmoSetDef UsedGenericAmmoSet => Props.ammoSet.similarTo ?? Props.ammoSet;

        public override List<AmmoSetDef> UsableAmmoSets
        {
            get
            {
                if (usableAmmoSets.NullOrEmpty())
                {
                    foreach (var def in DefDatabase<AmmoSetDef>.AllDefs)
                    {
                        if (def.similarTo == null)
                        {
                            continue;
                        }
                        if (def.similarTo == UsedGenericAmmoSet && !def.ammoTypes.First().ammo.menuHidden && !IsIdenticalToAny(def))
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
