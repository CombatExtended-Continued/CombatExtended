using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended.AI
{
    public class RaidGoalFinder_HostilePawns : RaidGoalFinder
    {
        protected override List<TargetInfo> FindTargetsFor(Faction faction, Map map)
        {
            List<Pawn> hostileList = map.mapPawns.AllPawnsSpawned.FindAll(p => p.HostileTo(faction));
            List<TargetInfo> targetList = new List<TargetInfo>();
            foreach(Pawn pawn in hostileList)
            {
                targetList.Add(new TargetInfo(pawn));
            }
            return targetList;
        }
    }
}
