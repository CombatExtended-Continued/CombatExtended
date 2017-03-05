using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;

namespace CombatExtended
{
	public class FactionBrainManager : MapComponent
	{
        private Dictionary<Faction, FactionBrain> brains = new Dictionary<Faction, FactionBrain>();

        public FactionBrainManager(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.LookDictionary(ref brains, "brains", LookMode.Deep);
            foreach(var fac in brains)
            {
                fac.Value.manager = this;
            }
        }

        public override void MapComponentTick()
        {
            List<Faction> facList = new List<Faction>() { Capacity = Find.FactionManager.AllFactions.Count() } ;
            foreach(var entry in brains)
            {
                var brain = entry.Value;
                if (brain.ShouldDelete) facList.Add(entry.Key);
                else if (Find.TickManager.TicksGame % brain.TickInterval == 0) brain.BrainTick();
            }
            foreach(var fac in facList)
            {
                brains.Remove(fac);
            }
        }

        public FactionBrain GetBrainFor(Faction fac)
        {
            FactionBrain brain;
            if (!brains.TryGetValue(fac, out brain))
            {
                // Create new brain if we don't already have one
                brain = new FactionBrain(this, fac);
                brains.Add(fac, brain);
            }
            return brain;
        }
    }
}
