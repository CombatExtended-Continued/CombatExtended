using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended.AI
{
    public class SquadBrainManager : MapComponent
    {
        List<SquadBrain> squadBrains = new List<SquadBrain>();

        public SquadBrainManager(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.LookList<SquadBrain>(ref squadBrains, "squadBrains", LookMode.Deep, new object[0]);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                for (int i = 0; i < squadBrains.Count; i++)
                {
                    squadBrains[i].squadBrainManager = this;
                }
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            foreach(SquadBrain brain in squadBrains)
            {
                brain.SquadBrainTick();
            }
        }
    }
}
