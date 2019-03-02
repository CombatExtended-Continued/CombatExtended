using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class CompProperties_Charges : CompProperties
    {
        // Charges are paired as velocity / range
        public List<int> chargeSpeeds = new List<int>();

        public CompProperties_Charges()
        {
            compClass = typeof(CompCharges);
        }
    }
}
