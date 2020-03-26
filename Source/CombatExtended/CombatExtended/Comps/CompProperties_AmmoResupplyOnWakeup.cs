using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class CompProperties_AmmoResupplyOnWakeup : CompProperties
    {
        public bool dropInPods;
        
        public CompProperties_AmmoResupplyOnWakeup()
        {
            compClass = typeof(CompAmmoResupplyOnWakeup);
        }
    }
}
