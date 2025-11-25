using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
//using Reloader;
using UnityEngine;
using System.Reflection;

namespace CombatExtended.AI;
public class SquadPathMover
{
    private enum states
    {
        moving = 1, ideal = 2, findingPath = 3
    }

    private readonly Map map;

    private IntVec3 target;
    private Region targetRegion;

    private List<Region> PathOfSquad;

    private List<Pawn> enemyPawns;

    private readonly Dictionary<Pawn, IntVec3> squadTargets;

    private List<Pawn> squadPawns;

    private readonly Faction fac;

    private states state =
        states.ideal;

    public SquadPathMover(Map map, Faction fac)
    {
        this.map
            = map;

        this.fac
            = fac;

        this.squadPawns
            = new List<Pawn>();
    }

    public void RegisterTarget(IntVec3 target)
    {
        this.target
            = target;

        this.targetRegion
            = target.GetRegion(map);

        this.OnTargetSet();

        this.state =
            states.findingPath;
    }

    public void addPawn(Pawn pawn)
    {
        this.squadPawns
        .Add(pawn);
    }

    public void removePawn(Pawn pawn)
    {
        if (!squadPawns.Contains(pawn))
        {
            throw new NullReferenceException();
        }

        this.squadPawns.Remove(pawn);

        this.squadTargets.Remove(pawn);
    }

    private long tick_counts = 0;

    public void TickPather ()
    {
        switch (tick_counts++ % 30)
        {
            case 0:
                this.CheckPathSafty();
                break;
            case 1:
                this.CheckForEnemyPawns();
                break;
            default:
                break;
        }

        if (tick_counts % 100 == 0)
        {
            Log.Message("state number is " + state);
        }

        if (state == states.findingPath)
        {
            this.FindPathSafe();

            this.state
                = states.moving;
        }

        if (tick_counts % 5 != 0)
        {
            return;
        }

        if (state == states.moving)
        {
            this.Move();
        }
    }

    private void Move()
    {
        if (PathOfSquad.Count == 0)
        {
            this.state
                = states.ideal;

            return;
        }

        if (PathOfSquad[0] == null)
        {
            squadPawns.RemoveAt(0);
            return;
        }

        foreach (Pawn pawn in squadPawns)
        {
            try
            {
                if (pawn.Position.GetRegion(map) != PathOfSquad[0])
                {
                    if (pawn.CurJob == null)
                    {
                        var region = PathOfSquad[0];

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
                             pawn.CurJob.targetA.Cell.GetRegion(map) != PathOfSquad[0])
                    {

                        var region = PathOfSquad[0];

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
            if (pawn.Position.GetRegion(map) != PathOfSquad[0])
            {
                return;
            }
        }

        PathOfSquad.RemoveAt(0);
    }



    //TODO FINISH EVENT SYSTEM.................................................................................

    #region END EVENTS-----------------------------------------------------------------------------------------

    public virtual void OnEnemyNearPawns(Pawn pawn, Pawn enemy)
    {

    }

    public virtual void OnEnemyNearPath(Region region)
    {

    }

    public virtual void OnMove()
    {

    }

    public virtual void OnTargetSet()
    {

    }

    public virtual void OnPawnInjured()
    {

    }

    public virtual void OnDistenatinReached()
    {

    }

    #endregion // END EVENTS-----------------------------------------------------------------------------------

    //TODO FINISH EVENT SYSTEM.................................................................................

    private void FindPathSafe ()
    {
        if(!this.target.IsValid)
        {
            throw new NullReferenceException();
        }

        SquadPather pather =
            new SquadPather(map);

        this.PathOfSquad = pather.GetSquadPathFromTo(squadPawns[0].Position.GetRegion(map)
                           , targetRegion
                           , fac, 0f).nodes;
    }

    private void CheckForEnemyPawns()
    {
        this.enemyPawns = map.mapPawns
                          .AllPawnsSpawned
                          .FindAll(t => fac.HostileTo(t.Faction));

        foreach (Pawn pawn in squadPawns)
        {
            foreach (Pawn enemy in enemyPawns)
            {
                if (Math.Sqrt(Math.Pow(pawn.Position.x + enemy.Position.x, 2) + Math.Pow(pawn.Position.y + enemy.Position.y, 2)) < 35f
                        &&
                        GenSight.LineOfSight(pawn.Position, enemy.Position, map, true))
                {
                    this.OnEnemyNearPawns(pawn, enemy);
                }
            }
        }
    }

    private void CheckPathSafty()
    {
        if (PathOfSquad == null)
        {
            return;
        }

        if (PathOfSquad.Count == 0)
        {
            return;
        }

        //TODO Tweaking Don't Forget About this

        if (PathOfSquad.Count <= 2)
        {
            return;
        }

        Region region =
            this.PathOfSquad[2];

        if (region == null)
        {
            return;
        }

        var safety =
            this.StudyWeaponSights(region);

        if (safety < 10)
        {
            return;
        }

        this.OnEnemyNearPath(region);
    }

    //[Reloader.ReloadMethod("SquadPathMover", "StudyWeaponSights", new Type[1] { typeof(Region)})]
    private float StudyWeaponSights(Region startRegion)
    {

        //avg value of cost..
        var sum = 0f;

        var count = 0;

        foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned.FindAll(t => fac.HostileTo(t.Faction)))
        {
            try
            {
                if (pawn.Faction == null)
                {


                    continue;
                }

                if (!pawn.Faction.HostileTo(fac))
                {


                    continue;
                }

                if (pawn.equipment == null)
                {


                    continue;
                }

                if (!pawn.equipment.HasAnything())
                {


                    continue;
                }

                if (pawn.equipment.PrimaryEq == null)
                {


                    continue;
                }

                if (pawn.equipment.PrimaryEq.PrimaryVerb == null)
                {


                    continue;
                }

                if (pawn.equipment.PrimaryEq.PrimaryVerb.verbProps == null)
                {


                    continue;
                }

            }
            catch (Exception er)
            {
                Log.Message(er.ToString());

                continue;
            }

            if (count > squadPawns.Count / 2 && squadPawns.Count > 1)
            {
                break;
            }

            Region targetReg =
                pawn.Position.GetRegion(pawn.Map);

            if (Math.Sqrt(
                        Math.Pow(targetReg.Cells.First().x - startRegion.Cells.First().x, 2)
                        +
                        Math.Pow(targetReg.Cells.First().y - startRegion.Cells.First().y, 2) - 10f
                    ) <= pawn.equipment.PrimaryEq.PrimaryVerb.verbProps.range)

            {
                for (int i = 0, j = 0; i < 4 + 1 && j < 4; i++)
                {
                    IntVec3 targetPoint =
                        startRegion.RandomCell;

                    IntVec3 startPoint =
                        targetReg.RandomCell;

                    //TODO Implment Range of weapons

                    if (GenSight.LineOfSight(startPoint
                                             , targetPoint
                                             , map
                                             , true))
                    {
                        sum += 100f;

                        j++;

                        count++;

                        break;
                    }
                }
            }

            if (pawn.equipment.PrimaryEq.PrimaryVerb.verbProps.range * 2f
                    > Math.Sqrt(
                        Math.Pow(targetReg.Cells.First().x - startRegion.Cells.First().x, 2)
                        +
                        Math.Pow(targetReg.Cells.First().y - startRegion.Cells.First().y, 2)
                    )
               )
            {
                var temp = (
                               pawn.equipment.PrimaryEq.PrimaryVerb.verbProps.range * 2f -
                               (float)Math.Sqrt(
                                   Math.Pow(targetReg.Cells.First().x - startRegion.Cells.First().x, 2)
                                   +
                                   Math.Pow(targetReg.Cells.First().y - startRegion.Cells.First().y, 2)
                               )
                           );

                for (int i = 0, j = 0; i < 4 + 1 && j < 4; i++)
                {
                    IntVec3 targetPoint =
                        startRegion.RandomCell;

                    IntVec3 startPoint =
                        targetReg.RandomCell;

                    //TODO Implment Range of weapons

                    if (GenSight.LineOfSight(startPoint
                                             , targetPoint
                                             , map
                                             , true))
                    {
                        sum += (float)temp;

                        j++;

                        count++;

                        break;
                    }
                }
            }
        }

        return sum;
    }
}
