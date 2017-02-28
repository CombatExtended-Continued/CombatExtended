using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended.AI
{
	public partial class SquadPather
	{
		//this is used to mark the number of passes over certin region
		private int[] DoneRegion;

		//This is the destination Region TODO Rename it
		public readonly Region des;

		private Faction fac;

		private List<Pawn> pawns;
		private List<Pawn> allPawns;

		private const int TEST_CELL_CONST
						= 10;

		private float fortificationStrength;
		private float defenderStrength;

		private List<Building_TurretGun> turrents;


		private SquadPath GetPathFromTo(Region startRegion, Region targetRegion, Faction fac, float fortLimit)
		{ 
			

			//TODO get the list of all pawn in the map
			//TODO (Filter Pawns that are Hostiles to the faction TODO Optimize)

			this.allPawns =
				map.mapPawns.AllPawns.ToList();

			this.pawns =
					map.mapPawns
					.AllPawnsSpawned.FindAll(p => p.Faction.HostileTo(fac))
					.ToList();

			List<Region> Path =
				GetPathAStar(startRegion
							 , targetRegion
							 , fac
							 , fortLimit).ToList();

			var squadPath =
				new SquadPath();

			squadPath.defenderStrength =
				         (float) Math.Pow(defenderStrength,2);
			
			squadPath.fortificationStrength =
				         fortificationStrength;


			Path.Reverse();

			squadPath.nodes = Path;

			return squadPath;
		}

		private float StudyRegion(Region region, Faction fac, float fortLimit)
		{
			//TODO NOW ..... LOL

			var temp = -StudyWalkSpeedRegion(region)
				- StudyObjectsCount(region)
				+ StudyWeaponSights(region)
				+ (float) fac.TacticalMemory
				             .TrapMemories()
				             .FindAll(
					             t => t.loc.GetRegion(t.map).Equals(region)
					            ).Count;

			fortificationStrength +=
				temp;

			defenderStrength +=
				(float) Math.Round(
					Math.Sqrt((float)fortificationStrength
					         )
				);

			return temp;
		}

		private float StudyWalkSpeedRegion(Region region)
		{
			//avg value of cost..
			var sum = 0f;

			//total number of cells..
			var total = 0f;

			foreach (IntVec3 cell in region.Cells)
			{
				sum += cell.GetTerrain(map)
						   .pathCost;
				total += 1;
			}

			return sum / total;
		}

		private float StudyObjectsCount(Region region)
		{
			//avg value of cost..
			var sum = 0f;

			//total number of cells..
			var total = 0f;

			foreach (Thing thing in region.ListerThings.AllThings)
			{
				sum += thing.def.fillPercent * 10f;
				total += 1;
			}

			return sum / total;
		}

		private float StudyWeaponSights(Region startRegion)
		{

			//avg value of cost..
			var sum = 0f;

			foreach (Pawn pawn in pawns)
			{
				if (pawn.equipment.PrimaryEq == null)
					continue;

				Region targetRegion = 
					pawn.Position.GetRegion(pawn.Map);

				if (pawn.equipment.PrimaryEq.PrimaryVerb.verbProps.range
					< Math.Sqrt(
						Math.Pow(targetRegion.Cells.First().x - startRegion.Cells.First().x, 2)
								+
						Math.Pow(targetRegion.Cells.First().y - startRegion.Cells.First().y, 2)
					   )
				   )
				{
					sum -=
						5.0000f;
					
					continue;
				}


				for (int i = 0; i < TEST_CELL_CONST; i++)
				{
					IntVec3 targetPoint =
						startRegion.RandomCell;

					IntVec3 startPoint = 
						startRegion.RandomCell;

					//TODO Implment Range of weapons

					if (GenSight.LineOfSight(startPoint
					                         ,targetPoint
					                         ,map
					                         ,true))
					{
						sum +=
							2.500000f;
					}
				}
			}

			return sum;
		}

		private IEnumerable<Region> GetPathAStar(Region startRegion, Region targetRegion, Faction fac, float fortLimit)
		{
			this.DoneRegion =
					new int[map.regionGrid.AllRegions.Count()];

			BetterPQueue<RegionNode> Queue =
				new BetterPQueue<RegionNode>();

			var startnode =
				RegionNode.CreateNode(startRegion, 0f, null);

			Queue.Push(startnode, startnode.Score);

			while (!Queue.isEmpty())
			{
				var node =
					Queue.getMin();

				if (node.CurrentRegion.mapIndex == targetRegion.mapIndex)
				{
					Queue.Push(node,node.Score);

					while (node != null)
					{
						yield return node.CurrentRegion;
						node = node.Parent;
					}

					break;
				}

				this.DoneRegion[node.CurrentRegion.mapIndex]++;

				foreach (RegionLink link in node.CurrentRegion.links)
				{
					var newregion =
						GetRegionLink(node.CurrentRegion, link);

					if (DoneRegion[newregion.mapIndex] > 0)
						continue;

					var temp = RegionNode.CreateNode(newregion
													 , StudyRegion(
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
