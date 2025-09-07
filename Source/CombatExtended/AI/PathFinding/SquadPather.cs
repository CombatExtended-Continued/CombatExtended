using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended.AI;
public partial class SquadPather
{
    private readonly Map map;

    private readonly int mapSizeX;

    private readonly int mapSizeZ;

    public SquadPather(Map map)
    {
        this.map
            = map;

        var mapSizePowTwo =
            map.info.PowerOfTwoOverMapSize;

        mapSizeX =
            map.Size.x;
        mapSizeZ =
            map.Size.z;
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
        var StartRegionX = startRegion.Cells.First().x;
        var StartRegionY = startRegion.Cells.First().z;

        //Get the End Region Position TODO may need swaping
        var EndRegionX = targetRegion.Cells.First().x;
        var EndRegionY = targetRegion.Cells.First().z;

        //Need Some Tweaking TODO Tweak distance
        return Math.Sqrt(Math.Pow((double)(StartRegionX - EndRegionX), 2.0)
                         + Math.Pow((double)(StartRegionY - EndRegionY), 2.0));
    }

    private IntVec3 GetRegionLocation(Region region)
    {
        var RegionX = (int)region.mapIndex / (int)mapSizeX;
        var RegionZ = region.mapIndex % mapSizeZ;

        return new IntVec3(RegionX, 0, RegionZ);
    }
}
