using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace CombatExtended.AI;
public class SquadBrainPlus
{
    private readonly Map map;
    private readonly Faction fac;

    private long tick_count;

    private List<Pawn> squadPawns;

    private Dictionary<Pawn, IntVec3> targets;

    private SquadPath path;

    private Region target;

    private enum states
    {
        moving = 1, ideal = 2, findingPath = 3
    }

    private states state =
        states.ideal;

    public SquadBrainPlus(Map map, Faction fac)
    {
        this.map
            = map;
        this.tick_count
            = 0;
        this.fac
            = fac;

        this.squadPawns
            = map.mapPawns
              .AllPawnsSpawned.FindAll((obj) => obj.Faction == Faction.OfPlayer);
        this.targets
            = new Dictionary<Pawn, IntVec3>();

        Log.Message("Pawn number is " +  squadPawns.Count);
    }

    public void Tick()
    {
        if (tick_count % 100 == 0)
        {
            Log.Message("state number is " + state);
        }

        if(tick_count++ % 10 == 0)
        {
            this.SearchForTarget();
        }

        if (state == states.findingPath)
        {
            this.FindPath();

            this.state
                = states.moving;
        }

        if (tick_count % 6 == 0)
        {

        }

        if (tick_count % 5 != 0)
        {
            return;
        }

        if (state == states.ideal)
        {
            this.Ideal();
        }

        if (state == states.moving)
        {
            this.Move();
        }
    }

    private void SearchForTarget()
    {
        var targetbed = map.listerThings
                        .AllThings
                        .Find(
                            (obj) =>
                            obj.def.IsBed
                        );

        this.target
            = targetbed.GetRegion();

        if (path == null)
        {
            this.state
                = states.findingPath;

            return;
        }

        if (path.nodes.Count == 0)
        {
            this.state
                = states.findingPath;
        }

        if (this.target == targetbed.GetRegion())
        {
            return;
        }
    }

    private void Move()
    {
        if (path.nodes.Count == 0)
        {
            this.state
                = states.ideal;

            return;
        }

        if (path.nodes[0] == null)
        {
            path.nodes.RemoveAt(0);
            return;
        }

        foreach (Pawn pawn in squadPawns)
        {
            try
            {
                if (pawn.Position.GetRegion(map) != path.nodes[0])
                {
                    if (pawn.CurJob == null)
                    {
                        var region = path.nodes[0];

                        var targetCell =
                            region.RandomCell;

                        while (!targetCell.Walkable(map))
                        {
                            targetCell = region.AnyCell;
                        }

                        pawn.jobs.StartJob(
                            new Job(JobDefOf.Goto, targetCell)
                        );
                    }
                    else if (pawn.CurJob.def != JobDefOf.Goto ||
                             pawn.CurJob.targetA.Cell.GetRegion(map) != path.nodes[0])
                    {

                        var region = path.nodes[0];

                        var targetCell =
                            region.RandomCell;

                        while (!targetCell.Walkable(map))
                        {
                            targetCell = region.RandomCell;
                        }

                        pawn.jobs.StartJob(
                            new Job(JobDefOf.Goto, targetCell)
                        );

                    }
                }
            }
            catch (Exception er)
            {
                Log.Message(er.ToString());
            }
        }

        foreach (Pawn pawn in squadPawns)
        {
            if (pawn.Position.GetRegion(map) != path.nodes[0])
            {
                return;
            }
        }

        path.nodes.RemoveAt(0);
    }

    private void Ideal()
    {

    }

    private void FindPath()
    {

        this.squadPawns
            = map.mapPawns
              .AllPawnsSpawned.FindAll((obj) => obj.Faction == Faction.OfPlayer);

        Log.Message("Starting PathFinding");

        SquadPather pather =
            new SquadPather(map);


        Log.Message("Starting PathFinding" + squadPawns[0]
                    .Position.GetRegion(map)
                    .Equals(this.target)
                   );

        this.path = pather.GetSquadPathFromTo(
                        squadPawns[0].Position.GetRegion(map),
                        this.target,
                        this.fac,
                        100
                    );

        Log.Message("nodes Counts " + this.path.nodes.Count);
    }

    private void AssignJobToPawn(Job job, Pawn pawn)
    {
        if (pawn.drafter != null)
        {
            pawn.jobs.TryTakeOrderedJob(job);
        }
        else
        {
            ExternalPawnDrafter.TakeOrderedJob(pawn, job);
        }
    }
}
