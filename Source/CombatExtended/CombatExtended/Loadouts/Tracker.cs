using System.Text.RegularExpressions;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace CombatExtended;
/// <summary>
/// Contains a series of HoldRecord items.  Functionally not that different from a List of HoldRecord but that list cannot be saved, this can be.
/// </summary>
public class Tracker : IExposable, ILoadReferenceable
{
    #region Fields
    private List<HoldRecord> _recs;
    internal int uniqueID;
    #endregion Fields

    #region Constructors
    public Tracker()
    {
        // this constructor is also used by the scribe.
        uniqueID = LoadoutManager.GetUniqueTrackerID();
        _recs = new List<HoldRecord>();
    }

    public Tracker(List<HoldRecord> newRecs)
    {
        uniqueID = LoadoutManager.GetUniqueTrackerID();
        _recs = newRecs;
    }
    #endregion Constructors

    #region Properties
    public List<HoldRecord> recs => _recs;
    #endregion Properties

    #region Methods
    /// <summary>
    /// Handles the save/load process as part of IExplosable.
    /// </summary>
    public void ExposeData()
    {
        // basic info about this loadout
        Scribe_Values.Look(ref uniqueID, "TrackerID");
        Scribe_Collections.Look(ref _recs, "HoldRecords");
    }

    /// <summary>
    /// Used by save/load to uniquely identify this instance.
    /// </summary>
    /// <returns></returns>
    public string GetUniqueLoadID()
    {
        return "Loadout_" + uniqueID;
    }
    #endregion Methods
}
