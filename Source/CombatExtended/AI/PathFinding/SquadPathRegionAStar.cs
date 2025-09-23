using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended.AI;
public sealed partial class SquadPather
{
    //this is used to mark the number of passes over certin region

    private Dictionary<int, int> DoneRegion;

    //This is the destination Region TODO Rename it
    public readonly Region des;

    private Faction fac;

    private List<Pawn> pawns;

    private const int TEST_CELL_CONST = 4;

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
            catch (Exception)
            {
                RemoverList.Add(pawn);

                continue;
            }
        }

        foreach (Pawn pawn in RemoverList)
        {
            pawns.Remove(pawn);
        }

        RemoverList.Clear();

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
            {
                temp.Add(i);
            }

            temp.Reverse();

            foreach (int i in temp)
            {
                Path.RemoveAt(i);
            }

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

        var density =
            StudyObjectsCount(region);

        if (Math.Abs(temp) <= 5)
        {
            return density;
        }

        var speed =
            +StudyWalkSpeedRegion(region);

        if (temp > 50f)
        {
            temp -= density +
                    speed;
        }

        return temp;
    }

    private float StudyWalkSpeedRegion(Region region)
    {
        return (float)(region.Cells.Count() - region.ListerThings.AllThings.Count);
    }

    private float StudyObjectsCount(Region region)
    {
        return region.ListerThings.AllThings.Count;
    }

    private float StudyWeaponSights(Region startRegion)
    {

        //avg value of cost..
        var sum = 0f;

        var count = 0;

        foreach (Pawn pawn in pawns)
        {
            if (count > pawns.Count / 2 && pawns.Count > 1)
            {
                break;
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
                        sum += 12;

                        j++;

                        count++;

                        break;
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

                        count++;
                    }
                }
            }
        }

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

        var breaK = false;

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

            if (node.CurrentRegion.links.Count == 2)
            {
                List<Region> temp =
                    new List<Region>();

                var region = node.CurrentRegion;

                DoneRegion.Remove(region.id);

                while (region.links.Count == 2)
                {
                    if (region.id == targetRegion.id)
                    {
                        while (temp.Count != 0)
                        {
                            yield return temp.Last();
                            temp.RemoveLast();
                        }

                        while (node != null)
                        {
                            yield return node.CurrentRegion;

                            node = node.Parent;
                        }

                        breaK = true;

                        break;
                    }

                    if (breaK)
                    {
                        break;
                    }

                    if (DoneRegion.ContainsKey(region.id))
                    {
                        break;
                    }

                    DoneRegion.Add(region.id, 1);

                    if (!DoneRegion.ContainsKey(GetRegionLink(region, region.links.ToList()[0]).id))
                    {
                        temp.Add(GetRegionLink(region, region.links.First()));
                        region = GetRegionLink(region, region.links.First());
                    }
                    else if (!DoneRegion.ContainsKey(GetRegionLink(region, region.links.ToList()[1]).id))
                    {
                        temp.Add(GetRegionLink(region, region.links.First()));
                        region = GetRegionLink(region, region.links.First());
                    }
                    else
                    {
                        break;
                    }
                }

                if (!breaK)
                {
                    if (region.links.Count > 2)
                    {
                        var sumSight = (float)pawns.FindAll((obj) => temp.Contains(obj.GetRegion())).Count * 100;

                        foreach (Region r in temp)
                        {
                            sumSight += StudyObjectsCount(r) + StudyWalkSpeedRegion(r);
                        }

                        var Rtemp = RegionNode.CreateNode(temp[0],
                                                          sumSight
                                                          + (float)GetOclicdianDistanceRegionAtoB(temp[0], targetRegion)
                                                          + (float)GetOclicdianDistanceRegionAtoB(temp[0], node.CurrentRegion)
                                                          , node);

                        temp.RemoveAt(0);

                        while (temp.Count > 0)
                        {
                            Rtemp = RegionNode.CreateNode(temp[0],
                                                          sumSight
                                                          + (float)GetOclicdianDistanceRegionAtoB(temp[0], targetRegion)
                                                          + (float)GetOclicdianDistanceRegionAtoB(temp[0], Rtemp.CurrentRegion)
                                                          , Rtemp);

                            temp.RemoveAt(0);
                        }

                        Queue.Push(Rtemp, Rtemp.Score);
                    }
                    else if (region.links.Count == 1)
                    {
                        continue;
                    }
                }
            }
            else
            {
                foreach (RegionLink link in node.CurrentRegion.links)
                {
                    var newregion =
                        GetRegionLink(node.CurrentRegion, link);

                    if (DoneRegion.ContainsKey(newregion.id))
                    {
                        continue;
                    }

                    var temp = RegionNode.CreateNode(newregion,
                                                     StudyRegion(
                                                         newregion,
                                                         fac,
                                                         fortLimit)
                                                     + node.Score
                                                     + (float)GetOclicdianDistanceRegionAtoB(newregion, targetRegion)
                                                     + (float)GetOclicdianDistanceRegionAtoB(newregion, node.CurrentRegion) + 10f
                                                     , node);

                    Queue.Push(temp, temp.Score);
                }


                foreach (IntVec3 cell in new IntVec3[]
            {
                //node.CurrentRegion.extentsLimit.TopRight
                //,node.CurrentRegion.extentsLimit.BottomLeft
                //,node.CurrentRegion.extentsLimit.Cells.First()
                //,node.CurrentRegion.extentsLimit.Cells.Last(),
                //TODO Need to be Tested for which one is right LOL
                node.CurrentRegion.extentsClose.TopRight
                , node.CurrentRegion.extentsClose.BottomLeft
                , node.CurrentRegion.extentsClose.Cells.First()
                    , node.CurrentRegion.extentsClose.Cells.Last()
                })
                {
                    if (cell.IsValid)
                    {
                        continue;
                    }

                    if (!cell.Walkable(map))
                    {
                        continue;
                    }

                    if (cell.GetRegion(map) == null)
                    {
                        continue;
                    }

                    if (DoneRegion.ContainsKey(cell.GetRegion(map).id))
                    {
                        continue;
                    }

                    var temp = RegionNode.CreateNode(cell.GetRegion(map),
                                                     StudyRegion(
                                                         cell.GetRegion(map),
                                                         fac,
                                                         fortLimit)
                                                     + node.Score
                                                     + (float)GetOclicdianDistanceRegionAtoB(cell.GetRegion(map), targetRegion)
                                                     + (float)GetOclicdianDistanceRegionAtoB(cell.GetRegion(map), node.CurrentRegion)
                                                     , node);

                    Queue.Push(temp, temp.Score);
                }
            }

            if (breaK)
            {
                break;
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
