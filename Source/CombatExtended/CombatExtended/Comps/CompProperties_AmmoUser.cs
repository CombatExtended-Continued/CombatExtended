using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class CompProperties_AmmoUser : CompProperties
    {
        public int magazineSize = 0;
        public float reloadTime = 1;
        public bool reloadOneAtATime = false;
        public bool throwMote = true;
        public AmmoSetDef ammoSet = null;
        public float loadedAmmoBulkFactor = 0f;

        public CompProperties_AmmoUser()
        {
            compClass = typeof(CompAmmoUser);
        }
    }
}
