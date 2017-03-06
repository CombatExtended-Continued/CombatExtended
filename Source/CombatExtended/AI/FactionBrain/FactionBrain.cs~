using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;

namespace CombatExtended
{
	using CombatExtended.AI;

	public class FactionBrain : IExposable
	{
		public FactionBrainManager manager;
		private Faction faction;

		private SquadBrain brain;

		public Map Map => manager.map;
		internal bool ShouldDelete => Map.mapPawns.SpawnedPawnsInFaction(faction).NullOrEmpty();
		internal int TickInterval { get { return 10; } }  // TODO this should return the amount of ticks between running BrainTick()

		public FactionBrain(FactionBrainManager manager, Faction faction)
		{
			this.manager = manager;
			this.faction = faction;

			try
			{
				brain = new SquadBrain(Map.mapPawns.AllPawnsSpawned.FindAll(t => faction == t.Faction)
									   , faction
									   , manager.map);

				foreach (Pawn p in manager.map.mapPawns.AllPawnsSpawned.FindAll(t => faction == t.Faction))
				{
					p.GetComp<CompSquadBrain>().squad = brain;
				}
			}
			catch (Exception er)
			{
				Log.Message(er.Message);
			}
		}

		public void BrainTick()
		{


			foreach (Pawn p in manager.map.mapPawns.AllPawnsSpawned.FindAll(t => faction == t.Faction))
			{
				if (p.TryGetComp<CompSquadBrain>() != null)
				{
					p.GetComp<CompSquadBrain>().squad = brain;
				}
				else
				{

				}
			}


		}

		public void ExposeData()
		{
			Scribe_Values.LookValue(ref faction, "faction", Faction.OfPlayer);
		}
	}
}
