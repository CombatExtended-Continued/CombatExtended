using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;

namespace CombatExtended.AI;
public class FactionBrainManager : MapComponent
{
    private List<FactionBrain> brains = new List<FactionBrain>();

    public FactionBrainManager(Map map) : base(map)
    {
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref brains, "brains", LookMode.Deep);
        Action action = delegate
        {
            foreach (var brain in brains)
            {
                brain.manager = this;
            }
        };
        LongEventHandler.ExecuteWhenFinished(action);
    }

    public override void MapComponentTick()
    {
        foreach(var brain in brains)
        {
            brain.BrainTick();
        }
    }
}
