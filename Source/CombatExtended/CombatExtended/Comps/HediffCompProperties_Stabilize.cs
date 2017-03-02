using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class HediffCompProperties_Stabilize : HediffCompProperties
    {
        public HediffCompProperties_Stabilize()
        {
            compClass = typeof(HediffComp_Stabilize);
        }
    }
}
