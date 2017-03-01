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
		private readonly Map map;

		private readonly Region[] regionGrid;

		private readonly int mapSizeX;

		private readonly int mapSizeZ;

		//Store the Links Bettween Regions..
		private readonly RegionLink[] regionLink;

		//Store all pawns and how they affect the Region Weight and value 
		// TODO Try to use a faster DataStructer and Orginze it...

		private readonly List<Pawn> listOfPawns;

		//Store all enemy pawns and how they affect the Region Weight and value 
		// TODO Try to use a faster DataStructer and Orginze it...

		private readonly List<Pawn> listofHostiles;

		//Store all friendly pawns and how they affect the Region Weight and value 
		// TODO Try to use a faster DataStructer and Orginze it...

		private readonly List<Pawn> listofFriends;

		public SquadPather(Map map)
		{
			this.map
				= map;

			this.listOfPawns
				= map.mapPawns.AllPawns.ToList();

			var mapSizePowTwo =
				map.info.PowerOfTwoOverMapSize;

			var gridSizeX =
				(ushort)mapSizePowTwo;
			var gridSizeZ =
				(ushort)mapSizePowTwo;

			mapSizeX =
				map.Size.x;
			mapSizeZ =
				map.Size.z;

			this.regionGrid = map.regionGrid.
				AllRegions.ToArray();
		}

		public SquadPath GetSquadPathFromTo(IntVec3 startPos, IntVec3 targetPos, Faction fac, float fortLimit)
		{
			return GetSquadPathFromTo(startPos.GetRegion(map), targetPos.GetRegion(map), fac, fortLimit);
		}

		public SquadPath GetSquadPathFromTo(Region startRegion, Region targetRegion, Faction fac, float fortLimit)
		{
			// TODO Calculate most efficient region-wise path for squad to reach their objective without exceeding the fortification limit
			return GetPathFromTo(startRegion, targetRegion, fac, fortLimit);
		}

		private float GetSquadPathScoreFor(Region region, out float fortStrength)
		{
			// TODO Algorithm to rate regions based on how difficult they would be to cross in a combat situation (available cover, enemy fortifications, expected defenders, etc.)
			throw new NotImplementedException();
		}

		//Get The Next Region to that Link TODO Thanks ZHentar
		private static Region GetRegionLink(Region region, RegionLink link) =>
			Equals(region, link.RegionA) ? link.RegionB : link.RegionA;

		//Use this to get the Distance Bettween two regien without using Region Link
		private double GetOclicdianDistanceRegionAtoB(Region startRegion, Region targetRegion)
		{
			//May be I need to swap them LOL
			//Get the End Region Position TODO may need swaping
			var StartRegionX =  (int) startRegion.mapIndex / (int) mapSizeX;
			var StartRegionY =  startRegion.mapIndex % mapSizeZ ;

			//Get the End Region Position TODO may need swaping
			var EndRegionX = (int) targetRegion.mapIndex / (int)  mapSizeX;
			var EndRegionY = targetRegion.mapIndex % mapSizeZ;

			//Need Some Tweaking TODO Tweak distance
			return Math.Sqrt(Math.Pow((double) (StartRegionX - EndRegionX), 2.0)
			                 + Math.Pow((double) (StartRegionY -   EndRegionY), 2.0));
		}

		private IntVec3 GetRegionLocation(Region region)
		{
			var RegionX = (int)region.mapIndex / (int)mapSizeX;
			var RegionZ = region.mapIndex % mapSizeZ;

			return new IntVec3(RegionX, 0, RegionZ);
		}
	}
}
