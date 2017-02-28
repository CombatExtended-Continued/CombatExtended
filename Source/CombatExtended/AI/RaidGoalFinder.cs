using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace CombatExtended.AI
{
    public abstract class RaidGoalFinder
    {
        protected abstract List<TargetInfo> FindTargetsFor(Faction faction, Map map);

        internal SquadObjective GetOrdersFor(SquadBrain raid)
        {
            throw new NotImplementedException();
        }
    }
}
