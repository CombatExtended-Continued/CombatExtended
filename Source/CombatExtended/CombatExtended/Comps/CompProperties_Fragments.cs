using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class CompProperties_Fragments : CompProperties
    {
        public List<ThingDefCountClass> fragments = new List<ThingDefCountClass>();
        public float fragSpeedFactor = 1f;
        public float fragShadowChance = 0.2f;
        public FloatRange fragAngleRange = new FloatRange(0.55f, 5.5f);

        public CompProperties_Fragments()
        {
            compClass = typeof(CompFragments);
        }
    }
}
