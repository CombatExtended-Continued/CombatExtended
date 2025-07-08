using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace CombatExtended.AI;
public enum Squad_Movment_Mode : byte
{
    dont_wait_for_others,
    wait_for_others,
}

public enum Squad_Combat_Mode : byte
{
    avoid_on_sight,
    attack_on_sight,
    smart_attack,
    avoid_attack,
}

public enum Squad_Think_Mode : byte
{
    wait_for_instruction,
    free_will
}

public class SquadBrain : IExposable
{
    private class JobNode
    {
        public List<Job> jobs = new List<Job>();
    }

    public bool COMBAT_MODE = true;

    public Squad_Combat_Mode squad_Combat_Mode = Squad_Combat_Mode.attack_on_sight;

    public Squad_Movment_Mode squad_Movment_Mode;

    public Squad_Think_Mode squad_Think_Mode = Squad_Think_Mode.wait_for_instruction;

    private Dictionary<Pawn, JobNode> squadOrdersForPawns;

    private List<Pawn> squadPawns = new List<Pawn>();

    private IEnumerable<Region> squadPath;

    private Map map;

    private Faction faction;

    public Job GetJobFor(Pawn pawn)
    {
        //this.squadPawns = map.mapPawns.AllPawnsSpawned.FindAll(t => faction == t.Faction);
        return CombatTick(pawn);
    }

    private Job GotoJob(IntVec3 target_cell) => new Job(JobDefOf.Goto, target_cell);

    private Job GotoWaitCombat() => new Job(JobDefOf.WaitCombat);

    private Job WaitJob() => new Job(JobDefOf.WaitWander);

    private float GetScoreEnemies(Pawn pawn) => SquadHelper.StudyWeaponSights(pawn.GetRegion(), map.mapPawns.AllPawnsSpawned, map, faction);

    public SquadBrain() { }

    public SquadBrain(IEnumerable<Pawn> squadPawns, Faction faction, Map map)
    {
        this.map
            = map;

        this.faction
            = faction;

        this.squadPawns.AddRange(squadPawns);

        this.squadOrdersForPawns
            = new Dictionary<Pawn, JobNode>();
    }

    public void SetTarget(Region target)
    {
        cur_target = target;
    }

    private JobDef JobForTarget;

    private Region start_Point;

    private Region target;

    private Region cur_target;

    private void FindPath()
    {
        if (this.cur_target != this.target)
        {
            cur_target = target;
        }

        SquadPather pather = new SquadPather(map);

        this.squadPath = pather.GetSquadPathFromTo(squadPawns.First().GetRegion()
                         , target, faction, 0f).nodes;

        if (squadPath.Count() == 0)
        {
            this.cur_target = null;

            this.target = null;
        }

        foreach (Pawn pawn in squadPawns)
        {

            if (!squadOrdersForPawns.ContainsKey(pawn))
            {
                squadOrdersForPawns.Add(pawn, new JobNode());

                squadOrdersForPawns[pawn].jobs = new List<Job>();
            }

            squadOrdersForPawns[pawn].jobs.Clear();

            foreach (Region region in this.squadPath)
            {
                squadOrdersForPawns[pawn].jobs.Add(GotoJob(region.RandomCell));
            }
        }
    }

    private float GetSaftyForPawns()
    {
        var temp = 0f;

        foreach (Pawn pawn in squadPawns)
        {
            temp += GetScoreEnemies(pawn);
        }

        return temp;
    }

    private Job CombatTick(Pawn pawn)
    {
        if (!squadOrdersForPawns.ContainsKey(pawn))
        {
            squadOrdersForPawns.Add(pawn, new JobNode());
        }

        var node = squadOrdersForPawns[pawn];

        if (node.jobs.Count == 0)
        {
            return this.Search(pawn);
        }

        if (node.jobs.Count > 0 && node.jobs.Count % 2 == 0)
        {
            var temp = node.jobs[0];

            node.jobs.RemoveAt(0);

            return temp;
        }

        var totalEnemies = 0;

        var enemy_in_sight = FindSightAndRange(pawn, out totalEnemies);

        if (totalEnemies > squadPawns.Count() + 3)
        {
            return new Job(JobDefOf.FleeAndCower);
        }

        if (totalEnemies == 0)
        {
            return Search(pawn);
        }

        var temp_X = node.jobs[0];

        node.jobs.RemoveAt(0);

        return temp_X;
    }

    private Job Search(Pawn pawn)
    {
        //float verbRange = pawn.equipment?.PrimaryEq?.PrimaryVerb.verbProps.range ?? 0;

        var AntiScore = 0;

        var Score = FindSightAndRange(pawn, out AntiScore);


        if (AntiScore > squadPawns.Count())
        {
            return new Job(JobDefOf.FleeAndCower);
        }

        if (AntiScore == 0 && Score == 0)
        {
            SquadPather pather = new SquadPather(map);

            Pawn enemy = null;

            var Min = 10000;

            var temp_x = 0;

            foreach (Pawn p in map.mapPawns.AllPawnsSpawned.FindAll(t => faction.HostileTo(t.Faction)))
            {
                var temp = FindSightAndRangeAll(p, out temp_x);

                if (temp < Min)
                {
                    enemy = p;
                    Min = temp;
                }
            }

            if (enemy == null)
            {
                return null;
            }

            if (Math.Sqrt(
                        Math.Pow(pawn.Position.x - enemy.Position.x, 2)
                        +
                        Math.Pow(pawn.Position.y - enemy.Position.y, 2)
                    ) <= getWeaponRange(pawn))
            {

                foreach (Pawn p in squadPawns)
                {
                    var jobs = squadOrdersForPawns[pawn].jobs;

                    Job combat = new Job(JobDefOf.WaitCombat);

                    combat.expireRequiresEnemiesNearby = false;

                    jobs.Add(combat);
                }
            }

            SquadPath path = pather.GetSquadPathFromTo(pawn.GetRegion(), enemy.GetRegion(), faction, 0f);

            if (path.nodes.Count > 0)
            {
                path.nodes.RemoveAt(0);
            }
            else
            {
                return null;
            }


            foreach (Pawn p in squadPawns)
            {
                var range = getWeaponRange(p);

                int region_num = (int)(range / 10f) + 1;

                squadOrdersForPawns[pawn].jobs.Clear();

                var jobs = squadOrdersForPawns[pawn].jobs;

                for (int i = 0; i < path.nodes.Count - region_num; i++)
                {
                    Job job = new Job(JobDefOf.Goto, path.nodes[i].RandomCell);

                    job.expireRequiresEnemiesNearby = false;

                    jobs.Add(job);

                    job = new Job(JobDefOf.WaitCombat);

                    jobs.Add(job);
                }

                jobs.Add(new Job(CE_JobDefOf.RunForCover));

                Job combat = new Job(JobDefOf.WaitCombat);

                combat.expireRequiresEnemiesNearby = false;

                jobs.Add(combat);
            }
        }

        if (squadOrdersForPawns[pawn].jobs.Count == 0)
        {
            this.Search(pawn);

            Job jb = new Job(JobDefOf.WaitCombat);

            jb.expiryInterval = 60 * 4;

            return jb;
        }

        Job temp_job = squadOrdersForPawns[pawn].jobs[0];

        squadOrdersForPawns[pawn].jobs.RemoveAt(0);

        return temp_job;
    }

    private float getWeaponRange(Pawn pawn)
    {

        var temp = 0f;

        try
        {
            if (pawn.equipment != null)
            {

                if (pawn.equipment.PrimaryEq != null)
                {
                    temp = pawn.equipment.PrimaryEq.PrimaryVerb.verbProps.range;
                }
            }
        }
        catch (Exception)
        {
            temp = 0;
        }

        return temp;
    }

    private int FindSightAndRange(Pawn pawn, out int IntRange)
    {
        var temp = 0;

        var intRange = 0;

        foreach (Pawn enemy in map.mapPawns.AllPawnsSpawned.FindAll(t => faction.HostileTo(t.Faction)))
        {
            if (GenSight.LineOfSight(pawn.Position
                                     , enemy.Position
                                     , map
                                     , true) == true && Math.Sqrt(
                        Math.Pow(pawn.Position.x - enemy.Position.x, 2)
                        +
                        Math.Pow(pawn.Position.y - enemy.Position.y, 2)
                    ) <= getWeaponRange(pawn))
            {
                temp++;
            }

            if (getWeaponRange(enemy)
                    >= Math.Sqrt(
                        Math.Pow(pawn.Position.x - enemy.Position.x, 2)
                        +
                        Math.Pow(pawn.Position.y - enemy.Position.y, 2)
                    ) && GenSight.LineOfSight(pawn.Position
                                              , enemy.Position
                                              , map
                                              , true))
            {
                intRange++;
            }
        }

        IntRange = intRange;

        return temp;
    }

    private int FindSightAndRangeAll(Pawn pawn, out int IntRange)
    {
        var temp = 0;

        var intRange = 0;

        foreach (Pawn enemy in map.mapPawns.AllPawnsSpawned)
        {
            if (enemy == pawn)
            {
                continue;
            }



            if (GenSight.LineOfSight(pawn.Position
                                     , enemy.Position
                                     , map
                                     , true) == true && Math.Sqrt(
                        Math.Pow(pawn.Position.x - enemy.Position.x, 2)
                        +
                        Math.Pow(pawn.Position.y - enemy.Position.y, 2)
                    ) <= getWeaponRange(pawn))
            {
                temp++;
            }

            if (getWeaponRange(enemy)
                    >= Math.Sqrt(
                        Math.Pow(pawn.Position.x - enemy.Position.x, 2)
                        +
                        Math.Pow(pawn.Position.y - enemy.Position.y, 2)
                    ) && GenSight.LineOfSight(pawn.Position
                                              , enemy.Position
                                              , map
                                              , true))
            {
                intRange++;
            }
        }

        IntRange = intRange;

        return temp;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref map, "map");
        Scribe_Values.Look(ref faction, "faction", Faction.OfPlayer);
        Scribe_Collections.Look(ref squadPawns, "squadPawns");
        Scribe_Collections.Look(ref squadOrdersForPawns, "orders");
    }
}
