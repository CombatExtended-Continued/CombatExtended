using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class HediffCompProperties_InfecterCE : HediffCompProperties
    {
        public float infectionChancePerHourUntended = 0.01f;

        public HediffCompProperties_InfecterCE()
        {
            this.compClass = typeof(HediffComp_InfecterCE);
        }
    }
}
