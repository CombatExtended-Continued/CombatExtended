using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    class LoadoutGenerator_AmmoSecondary : LoadoutGenerator_AmmoPrimary
    {
        /// <summary>
        /// Initializes availableDefs and adds the ammo types of all currently equipped sidearms
        /// </summary>
        protected override void InitAvailableDefs()
        {
            availableDefs = new List<ThingDef>();
            foreach (ThingWithComps eq in compInvInt.rangedWeaponList)
            {
                AddAvailableAmmoFor(eq);
            }
        }
    }
}
