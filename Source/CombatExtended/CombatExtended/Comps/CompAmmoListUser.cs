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
                    foreach (var def in Props.additionalAmmoSets)
                    {
                        if (!def.ammoTypes.First().ammo.menuHidden && !IsIdenticalToAny(def))
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
