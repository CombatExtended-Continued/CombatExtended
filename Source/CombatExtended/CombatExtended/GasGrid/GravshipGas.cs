using System.Collections.Generic;
using RimWorld.Planet;
using Verse;
using Waylith.Injection;

namespace CombatExtended;


public class GravshipGasEntry : IExposable
{
    public GasDef gas;
    public Dictionary<IntVec3, ushort> cells = [];

    public void ExposeData()
    {
        Scribe_Defs.Look(ref gas, "gas");
        Scribe_Collections.Look(ref cells, "cells", LookMode.Value, LookMode.Value);
    }
}

public static class GravshipGasExtensions
{
    extension(Gravship gravship)
    {

        [InjectedField]
        private List<GravshipGasEntry> GasCargoStored
        {
            get => default;
            set { }
        }

        public List<GravshipGasEntry> GasCargo
        {
            get => gravship.GasCargoStored;
            set => gravship.GasCargoStored = value;
        }
    }
}
