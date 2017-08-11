using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended.AI
{
    public class SquadPath
    {
        public float fortificationStrength = 0;
        public float defenderStrength = 0;
        public List<Region> nodes = new List<Region>();
    }
}
