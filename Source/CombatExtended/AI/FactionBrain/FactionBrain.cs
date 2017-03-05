using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;

namespace CombatExtended
{
	public class FactionBrain : IExposable
	{
        public FactionBrainManager manager;
        private Faction faction;

        public Map Map => manager.map;
        internal bool ShouldDelete => Map.mapPawns.SpawnedPawnsInFaction(faction).NullOrEmpty();
        internal int TickInterval { get { throw new NotImplementedException(); } }  // TODO this should return the amount of ticks between running BrainTick()

        public FactionBrain(FactionBrainManager manager, Faction faction)
        {
            this.manager = manager;
            this.faction = faction;
        }

        public void BrainTick()
        {
            // TODO add faction brain calculations
        }

        public void ExposeData()
        {
            Scribe_Values.LookValue(ref faction, "faction", Faction.OfPlayer);
        }
    }
}
