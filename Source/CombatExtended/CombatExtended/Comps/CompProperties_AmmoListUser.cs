using System.Collections.Generic;

namespace CombatExtended
{
    public class CompProperties_AmmoListUser : CompProperties_AmmoUser
    {
        public List<AmmoSetDef> additionalAmmoSets = new List<AmmoSetDef>();

        public bool allowSimilarAmmo = false;

        public CompProperties_AmmoListUser()
        {
            compClass = typeof(CompAmmoListUser);
        }
    }
}
