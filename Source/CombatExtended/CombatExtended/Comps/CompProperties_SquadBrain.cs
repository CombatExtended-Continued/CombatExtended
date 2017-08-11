using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class CompProperties_SquadBrain : CompProperties
    {
        public CompProperties_SquadBrain()
        {
            compClass = typeof(CompSquadBrain);
        }
    }
}
