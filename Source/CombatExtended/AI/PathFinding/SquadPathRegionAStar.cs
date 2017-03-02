using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended.AI
{
	public sealed partial class SquadPather
	{
		//this is used to mark the number of passes over certin region

		private Dictionary<int, int> DoneRegion;

		//This is the destination Region TODO Rename it
		public readonly Region des;

		private Faction fac;

		private List<Pawn> pawns;

		private const int TEST_CELL_CONST
						= 5;

		private SquadPath GetPathFromTo(Region startRegion, Region targetRegion, Faction fac, float fortLimit)
		{


			//TODO get the list of all pawn in the map
			//TODO (Filter Pawns that are Hostiles to the faction TODO Optimize)

			this.fac = fac;

			this.pawns =
					map.mapPawns.
					AllPawnsSpawned
					.FindAll((obj) => obj.Faction.HostileTo(fac))
					.ToList();



			List<Region> Path =
				GetPathAStar(startRegion
							 , targetRegion
							 , fac
							 , fortLimit).ToList();

			Path.Reverse();

			//Smooth Down the Path TODO Need Rework
			if (Path.Count != 0)
			{
				List<int> temp =
					new List<int>();

				for (int i = 0; i < Path.Count - 2; i += 2)
					temp.Add(i);

				temp.Reverse();

				foreach (int i in temp)
					Path.RemoveAt(i);

				temp.Clear();
			}

			var squadPath =
				new SquadPath();


			Log.Message("nodes num " + Path.Count);

			squadPath.nodes = Path;

			return squadPath;
		}

		private float StudyRegion(Region region, Faction fac, float fortLimit)
		{
			//TODO NOW ..... LOL

			var temp = StudyWeaponSights(region) + (float)fac.TacticalMemory
							 .TrapMemories()
							 .FindAll(
								 t => t.loc.GetRegion(t.map).Equals(region)
								).Count;

			if (Math.Abs(temp) <= 5)
				return 0f;
			
			//var speed =
			//	+StudyWalkSpeedRegion(region);

			//var density =
			//	StudyObjectsCount(region);

			//if (density > speed)
			//{
			//	density = speed;
			//}

			//if (temp > 50f)
			//{
			//	temp -= density +
			//		speed;
			//}

			return temp;
		}

		private float StudyWalkSpeedRegion(Region region)
		{
			//avg value of cost..
			var sum = 0f;

			//total number of cells..
			var total = 0f;

			for (int i = 0; i < TEST_CELL_CONST;i++)
			{
				sum += region.RandomCell.GetTerrain(map)
						   .pathCost;
				total += 1;
			}

			return sum / total + total;
		}

		private float StudyObjectsCount(Region region)
		{
			//total number of cells..
			var total = 0f;

			foreach (Thing thing in region.ListerThings.AllThings)
			{
				total += 1;
			}

			return total;
		}

		private float StudyWeaponSights(Region startRegion)
		{

			//avg value of cost..
			var sum = 0f;

			List<Pawn> RemoverList = 
				new List<Pawn>();

			foreach (Pawn pawn in pawns)
			{
				try
				{
					if (pawn.Faction == null)
					{
						RemoverList.Add(pawn);

						continue;
					}

					if (!pawn.Faction.HostileTo(fac))
					{
						RemoverList.Add(pawn);

						continue;
					}

					if (pawn.equipment == null)
					{
						RemoverList.Add(pawn);

						continue;
					}

					if (!pawn.equipment.HasAnything())
					{
						RemoverList.Add(pawn);

						continue;
					}

					if (pawn.equipment.PrimaryEq == null)
					{
						RemoverList.Add(pawn);

						continue;
					}

					if (pawn.equipment.PrimaryEq.PrimaryVerb == null)
					{
						RemoverList.Add(pawn);

						continue;
					}

					if (pawn.equipment.PrimaryEq.PrimaryVerb.verbProps == null)
					{
						RemoverList.Add(pawn);

						continue;
					}

				}
				catch (Exception er)
				{
					RemoverList.Add(pawn);

					continue;
				}

				Region targetRegion =
					pawn.Position.GetRegion(pawn.Map);

				if (Math.Sqrt(
							Math.Pow(targetRegion.Cells.First().x - startRegion.Cells.First().x, 2)
								+
							Math.Pow(targetRegion.Cells.First().y - startRegion.Cells.First().y, 2) - 10f
				) <= pawn.equipment.PrimaryEq.PrimaryVerb.verbProps.range)

				{
					for (int i = 0, j = 0; i < TEST_CELL_CONST + 1 && j < 4; i++)
					{
						IntVec3 targetPoint =
							startRegion.RandomCell;

						IntVec3 startPoint =
							targetRegion.RandomCell;

						//TODO Implment Range of weapons

						if (GenSight.LineOfSight(startPoint
												 , targetPoint
												 , map
												 , true))
						{
							sum += 100f;

							j++;
						}
					}
				}

				if (pawn.equipment.PrimaryEq.PrimaryVerb.verbProps.range * 2f
						> Math.Sqrt(
							Math.Pow(targetRegion.Cells.First().x - startRegion.Cells.First().x, 2)
									+
							Math.Pow(targetRegion.Cells.First().y - startRegion.Cells.First().y, 2)
						   )
					   )
				{
					var temp = (
						pawn.equipment.PrimaryEq.PrimaryVerb.verbProps.range * 2f -
						(float)Math.Sqrt(
							Math.Pow(targetRegion.Cells.First().x - startRegion.Cells.First().x, 2)
								+
							Math.Pow(targetRegion.Cells.First().y - startRegion.Cells.First().y, 2)
									)
					);

					for (int i = 0, j = 0; i < TEST_CELL_CONST + 1 && j < 4; i++)
					{
						IntVec3 targetPoint =
							startRegion.RandomCell;

						IntVec3 startPoint =
							targetRegion.RandomCell;

						//TODO Implment Range of weapons

						if (GenSight.LineOfSight(startPoint
												 , targetPoint
												 , map
												 , true))
						{
							sum += (float)temp;

							j++;
						}
					}
				}
			}

			foreach (Pawn pawn in RemoverList)
				pawns.Remove(pawn);

			RemoverList.Clear();

			return sum;
		}


		private IEnumerable<Region> GetPathAStar(Region startRegion, Region targetRegion, Faction fac, float fortLimit)
		{
			this.DoneRegion =
					new Dictionary<int, int>();

			BetterPQueue<RegionNode> Queue =
				new BetterPQueue<RegionNode>();

			var startnode =
				RegionNode.CreateNode(startRegion, 0f, null);

			Queue.Push(startnode, startnode.Score);

			while (!Queue.isEmpty())
			{
				var node =
					Queue.getMin();

				if (node.CurrentRegion.Equals(targetRegion))
				{

					Queue.Push(node, node.Score);

					while (node != null)
					{
						yield return node.CurrentRegion;

						node = node.Parent;
					}

					break;
				}

				if (!DoneRegion.ContainsKey(node.CurrentRegion.id))
				{
					this.DoneRegion.Add(node.CurrentRegion.id, 1);
				}

				foreach (RegionLink link in node.CurrentRegion.links)
				{
					var newregion =
						GetRegionLink(node.CurrentRegion, link);


					try
					{
						if (DoneRegion.ContainsKey(newregion.id))
							continue;
					}
					catch (Exception er)
					{
						Log.Message(er.ToString());
					}

					var temp = RegionNode.CreateNode(newregion,
													  StudyRegion(
														 newregion,
														 fac,
														 fortLimit)
													 + node.Score
													 + (float)GetOclicdianDistanceRegionAtoB(newregion, targetRegion)
													 + 10.00f
													 , node);

					Queue.Push(temp, temp.Score);
				}
			}

			yield return null;
		}

		class RegionNode
		{
			private readonly Region currentRegion;

			private readonly float score;

			private readonly RegionNode parent;

			public static RegionNode CreateNode(Region CurrentRegion, float Score, RegionNode parent)
			{
				return new RegionNode(CurrentRegion, Score, parent);
			}

			public static RegionNode CreateNode(Region CurrentRegion, float Score)
			{
				return new RegionNode(CurrentRegion, Score, null);
			}

			private RegionNode(Region CurrentRegion, float Score, RegionNode Parent)
			{
				this.currentRegion =
						CurrentRegion;
				this.score =
						Score;
				this.parent =
					Parent;
			}

			public Region CurrentRegion
			{
				get
				{
					return currentRegion;
				}
			}

			public float Score
			{
				get
				{
					return score;
				}
			}

			public RegionNode Parent
			{
				get
				{
					return parent;
				}
			}
		}

	}
}
