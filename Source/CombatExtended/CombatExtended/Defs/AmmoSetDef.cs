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
    }
}
