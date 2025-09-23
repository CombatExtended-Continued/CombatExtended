using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;

namespace CombatExtended.AI;
public class FactionBrain : IExposable
{
    public FactionBrainManager manager;
    private Faction faction;

    private SquadBrain brain;

    public Map Map => manager.map;
    internal bool ShouldDelete => Map.mapPawns.SpawnedPawnsInFaction(faction).NullOrEmpty();
    internal int TickInterval
    {
        get
        {
            return 10;    // TODO this should return the amount of ticks between running BrainTick()
        }
    }

    public FactionBrain()
    {
    }

    public FactionBrain(FactionBrainManager manager, Faction faction)
    {
        this.manager = manager;
        this.faction = faction;
        //LongEventHandler.ExecuteWhenFinished(InitBrain);
    }

    private void InitBrain()
    {
        try
        {
            List<Pawn> list = new List<Pawn>(manager.map.mapPawns.FreeHumanlikesSpawnedOfFaction(faction));
            brain = new SquadBrain(list
                                   , faction
                                   , manager.map);

            foreach (Pawn p in list)
            {
                CompSquadBrain comp = p.TryGetComp<CompSquadBrain>();
                if (comp != null)
                {
                    comp.squad = brain;
                }
            }
        }
        catch (Exception er)
        {
            Log.Message(er.Message);
        }
    }

    public void BrainTick()
    {
        if (brain == null)
        {
            InitBrain();
        }

        foreach (Pawn p in manager.map.mapPawns.FreeHumanlikesSpawnedOfFaction(faction))
        {
            CompSquadBrain comp = p.TryGetComp<CompSquadBrain>();
            if (comp != null && comp.squad == null)
            {
                comp.squad = brain;
            }
        }


    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref faction, "faction", Faction.OfPlayer);
    }
}
