using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended.AI
{
    public class SquadPather
    {
        private readonly Map map;

        public SquadPath GetSquadPathFromTo(IntVec3 startPos, IntVec3 targetPos, Faction fac, float fortLimit)
        {
            return GetSquadPathFromTo(startPos.GetRegion(map), targetPos.GetRegion(map), fac, fortLimit);
        }

        public SquadPath GetSquadPathFromTo(Region startRegion, Region targetRegion, Faction fac, float fortLimit)
        {
            // TODO Calculate most efficient region-wise path for squad to reach their objective without exceeding the fortification limit
            throw new NotImplementedException();
        }

        private float GetSquadPathScoreFor(Region region, out float fortStrength)
        {
            // TODO Algorithm to rate regions based on how difficult they would be to cross in a combat situation (available cover, enemy fortifications, expected defenders, etc.)
            throw new NotImplementedException();
        }
    }
}
