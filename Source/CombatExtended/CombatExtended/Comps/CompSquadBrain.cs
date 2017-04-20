using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using CombatExtended.AI;

namespace CombatExtended
{
    public class CompSquadBrain : ThingComp
    {
        public SquadBrain squad;

        CompProperties_SquadBrain Props => props as CompProperties_SquadBrain;
    }
}
