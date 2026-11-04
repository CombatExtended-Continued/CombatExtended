using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended;

public sealed class GasLayer(CEGasGrid grid, Map map, GasDef gasDef)
{
    #region Constants
    private const int OffMap = -1;

    // Ticks at the start of each interval over which its work is spread (1 to the interval length).
    private const int WorkTicksPerInterval = 15;

    // Smoke thinner than this fraction of the maximum is too faint to count as danger for the AI.
    private const float MinDangerConcentration = 0.01f;

    // Only gas thicker than this can leave filth behind as it dissipates.
    private const float MinDensityForFilth = 300f;
    #endregion

    #region Fields
    // Map
    private readonly int _mapWidth = map.Size.x;
    private readonly int _mapHeight = map.Size.z;

    // Density
    private readonly float[] _density = new float[map.cellIndices.NumGridCells];

    // Cells that hold (or recently held) smoke. A cell is listed as soon as it gains smoke and unlisted lazily at the
    // start of the next interval, which avoids a set lookup on every change.
    private readonly bool[] _isListedActive = new bool[map.cellIndices.NumGridCells];
    private readonly List<int> _activeCells = [];

    // Transfers are queued here during an interval and all applied at its end, see ApplyInterval.
    private readonly float[] _pendingChange = new float[map.cellIndices.NumGridCells];
    private readonly List<int> _cellsWithPendingChange = [];

    // The interval in progress is spread over several ticks, see Tick.
    private readonly List<int> _cellsThisInterval = [];
    private readonly List<int> _blockedCells = [];
    private int _nextCellInInterval;
    private bool _intervalInProgress;
    private DangerTracker _dangerTrackerThisInterval;
    #endregion

    #region Properties
    public GasDef Def { get; } = gasDef;

    public bool HasActiveCells => _activeCells.Count > 0;
    #endregion

    #region Density access
    public float DensityAt(IntVec3 cell)
    {
        return cell.InBounds(map) ? _density[map.cellIndices.CellToIndex(cell)] : 0f;
    }

    /// <summary>Density at an index into the map's cell arrays. The index must be inside the map.</summary>
    public float DensityAtIndex(int index) => _density[index];

    public void AddDensity(IntVec3 cell, float amount)
    {
        if (!cell.InBounds(map))
        {
            return;
        }
        CompletePendingInterval();
        int index = map.cellIndices.CellToIndex(cell);
        SetDensity(index, _density[index] + amount);
    }

    public void SetDensityAt(IntVec3 cell, float value)
    {
        if (cell.InBounds(map))
        {
            CompletePendingInterval();
            SetDensity(map.cellIndices.CellToIndex(cell), value);
        }
    }

    // Finish queued transfers before external edits or read-and-remove operations.
    public void CompletePendingInterval()
    {
        if (_intervalInProgress)
        {
            WorkSlice(1);
        }
    }

    public void RemoveAllGas()
    {
        System.Array.Clear(_density, 0, _density.Length);
        System.Array.Clear(_pendingChange, 0, _pendingChange.Length);
        System.Array.Clear(_isListedActive, 0, _isListedActive.Length);
        _activeCells.Clear();
        _cellsThisInterval.Clear();
        _cellsWithPendingChange.Clear();
        _blockedCells.Clear();
        _intervalInProgress = false;
        map.mapDrawer.WholeMapChanged(CE_MapMeshFlagDefOf.CE_Gas);
    }

    /// <summary>Whether the cell's puff would look different, so its map section needs redrawing.</summary>
    private bool LooksDifferent(float before, float after)
    {
        bool wasDrawn = Def.IsDrawn(before);
        bool isDrawn = Def.IsDrawn(after);
        return wasDrawn != isDrawn || (isDrawn && Def.ColorFor(before).a != Def.ColorFor(after).a);
    }

    private void SetDensity(int index, float value)
    {
        float before = _density[index];
        _density[index] = Mathf.Clamp(value, 0f, Def.maxDensity);
        if (LooksDifferent(before, _density[index]))
        {
            map.mapDrawer?.MapMeshDirty(map.cellIndices.IndexToCell(index), CE_MapMeshFlagDefOf.CE_Gas);
        }
        if (_density[index] > 0f && !_isListedActive[index])
        {
            _isListedActive[index] = true;
            _activeCells.Add(index);
        }
    }
    #endregion

    #region Saving
    /// <summary>
    /// Saved like vanilla's GasGrid: one value per cell, written as a single compact string. A layer with no smoke writes only a short flag instead of a whole-map block.
    /// </summary>
    public void ExposeData()
    {
        bool hasGas = Scribe.mode != LoadSaveMode.Saving || ContainsAnyGas();
        Scribe_Values.Look(ref hasGas, "gasPresent_" + Def.defName, true);
        if (!hasGas)
        {
            return;
        }

        MapExposeUtility.ExposeUshort(map,
            cell => Def.ToSaved(_density[map.cellIndices.CellToIndex(cell)]),
            (cell, saved) => SetDensity(map.cellIndices.CellToIndex(cell), Def.FromSaved(saved)),
            "gas_" + Def.defName);
    }

    // The active list can still hold cells that emptied since the last interval, so it has to look at the densities.
    private bool ContainsAnyGas()
    {
        for (int i = 0; i < _activeCells.Count; i++)
        {
            if (_density[_activeCells[i]] > 0f)
            {
                return true;
            }
        }
        return false;
    }
    #endregion

    #region Simulation
    /// <summary>
    /// Runs the simulation once every <see cref="CEGasGrid.UpdateIntervalTicks"/> ticks, spread over the first
    /// <see cref="WorkTicksPerInterval"/> ticks so a large cloud does not cost one long tick. Changes are queued and applied
    /// on the last slice, or before an external density edit finishes the interval early.
    /// </summary>
    public void Tick(int ticks)
    {
        int tickInInterval = ticks % CEGasGrid.UpdateIntervalTicks;
        if (tickInInterval == 0)
        {
            BeginInterval(ticks);
        }
        if (!_intervalInProgress)
        {
            return;
        }

        if (tickInInterval < WorkTicksPerInterval)
        {
            int workTicksRemaining = WorkTicksPerInterval - tickInInterval;
            WorkSlice(workTicksRemaining);
        }
    }

    private void BeginInterval(int ticks)
    {
        UnlistEmptiedCells();

        _intervalInProgress = _activeCells.Count > 0;
        if (!_intervalInProgress)
        {
            return;
        }

        _cellsThisInterval.Clear();
        _cellsThisInterval.AddRange(_activeCells);
        _nextCellInInterval = 0;
        _blockedCells.Clear();

        // Danger notifications are only needed every other interval.
        bool notifyDangerThisInterval = (ticks / CEGasGrid.UpdateIntervalTicks) % 2 == 0;
        _dangerTrackerThisInterval = notifyDangerThisInterval ? map.GetDangerTracker() : null;
    }

    private void UnlistEmptiedCells()
    {
        int kept = 0;
        for (int i = 0; i < _activeCells.Count; i++)
        {
            int index = _activeCells[i];
            if (_density[index] > 0f)
            {
                _activeCells[kept++] = index;
            }
            else
            {
                _isListedActive[index] = false;
            }
        }
        _activeCells.RemoveRange(kept, _activeCells.Count - kept);
    }

    private void WorkSlice(int workTicksRemaining)
    {
        int cellsLeft = _cellsThisInterval.Count - _nextCellInInterval;
        int cellsInSlice = (cellsLeft + workTicksRemaining - 1) / workTicksRemaining;
        int endOfSlice = _nextCellInInterval + cellsInSlice;

        for (int i = _nextCellInInterval; i < endOfSlice; i++)
        {
            ProcessCell(_cellsThisInterval[i]);
        }

        _nextCellInInterval = endOfSlice;
        bool isLastSlice = workTicksRemaining == 1;
        if (isLastSlice)
        {
            ApplyInterval();
        }
    }

    private void ProcessCell(int index)
    {
        float cellDensity = _density[index];
        if (cellDensity <= 0f)
        {
            return;
        }

        bool isThickEnoughToDissipateGradually = cellDensity > Def.dissipationThreshold;
        if (isThickEnoughToDissipateGradually)
        {
            cellDensity -= DensityDissipateGradually(index, cellDensity);
        }
        else if (Rand.Chance(Def.expireChancePerInterval))
        {
            QueueChange(index, -cellDensity);
            return;
        }

        if (!grid.CanMoveTo(index))
        {
            // The cell became inaccessible, probably a recently closed door or vent. Its smoke spreads out below
            // and the cell is emptied at the end of the interval.
            _blockedCells.Add(index);
        }
        else if (!map.roofGrid.Roofed(index))
        {
            cellDensity -= DensityLostToOpenAir(index, cellDensity);
        }

        NotifyDangerTracker(index);

        if (cellDensity >= Def.minSpreadDensity)
        {
            SpreadToNeighbors(index, cellDensity);
        }
    }

    private float DensityDissipateGradually(int index, float cellDensity)
    {
        float lostPerTick = Mathf.Max(Def.minDissipationPerTick, cellDensity * Def.dissipationRate);
        float lost = Mathf.Min(cellDensity, lostPerTick * CEGasGrid.UpdateIntervalTicks);

        bool leavesFilthThisInterval = Def.leaveFilth && cellDensity > MinDensityForFilth && Rand.Range(0, (int)Def.maxDensity) < lost;
        if (leavesFilthThisInterval)
        {
            FilthMaker.TryMakeFilth(map.cellIndices.IndexToCell(index), map, Def.filthDef);
        }

        QueueChange(index, -lost);
        return lost;
    }

    private float DensityLostToOpenAir(int index, float cellDensity)
    {
        float lost = Mathf.Min(Def.outdoorLossPerInterval, cellDensity);
        QueueChange(index, -lost);
        return lost;
    }

    private void NotifyDangerTracker(int index)
    {
        if (_dangerTrackerThisInterval == null)
        {
            return;
        }

        float concentration = _density[index] / Def.maxDensity;
        if (concentration >= MinDangerConcentration)
        {
            _dangerTrackerThisInterval.Notify_SmokeAt(map.cellIndices.IndexToCell(index), concentration);
        }
    }

    /// <summary>
    /// Moves half the difference in density to each neighbor that is lower, starting from a random direction and winding either way.
    /// </summary>
    private void SpreadToNeighbors(int index, float cellDensity)
    {
        int x = index % _mapWidth;
        int z = index / _mapWidth;

        int firstDirection = Rand.Range(0, Rot4.RotationCount);
        int directionStep = Rand.Bool ? 1 : -1;   // winds one way round or the other; Rot4 wraps the number into 0 to 3
        for (int i = 0; i < Rot4.RotationCount; i++)
        {
            var direction = new Rot4(firstDirection + (i * directionStep));
            int neighbor = NeighborIndex(index, x, z, direction);
            if (neighbor == OffMap || !grid.CanMoveTo(neighbor))
            {
                continue;
            }

            float neighborDensity = _density[neighbor];
            // Reserve space for transfers already queued for this neighbor.
            float projectedNeighborDensity = neighborDensity + _pendingChange[neighbor];
            float remainingCapacity = Mathf.Max(0f, Def.maxDensity - projectedNeighborDensity);
            float desiredTransfer = Mathf.Max(0f, (cellDensity - neighborDensity) * 0.5f);
            float transferred = Mathf.Min(desiredTransfer, remainingCapacity);
            if (transferred <= 0f)
            {
                continue;
            }

            QueueChange(index, -transferred);
            QueueChange(neighbor, transferred);
            cellDensity -= transferred;
        }
    }

    private int NeighborIndex(int index, int x, int z, Rot4 direction)
    {
        switch (direction.AsInt)
        {
            case Rot4.NorthInt:
                return z + 1 < _mapHeight ? index + _mapWidth : OffMap;
            case Rot4.EastInt:
                return x + 1 < _mapWidth ? index + 1 : OffMap;
            case Rot4.SouthInt:
                return z > 0 ? index - _mapWidth : OffMap;
            default:
                return x > 0 ? index - 1 : OffMap;   // West
        }
    }

    private void QueueChange(int index, float amount)
    {
        if (_pendingChange[index] == 0f)
        {
            _cellsWithPendingChange.Add(index);
        }
        _pendingChange[index] += amount;
    }

    private void ApplyInterval()
    {
        for (int i = 0; i < _cellsWithPendingChange.Count; i++)
        {
            int index = _cellsWithPendingChange[i];
            SetDensity(index, _density[index] + _pendingChange[index]);
            _pendingChange[index] = 0f;
        }
        _cellsWithPendingChange.Clear();

        for (int i = 0; i < _blockedCells.Count; i++)
        {
            SetDensity(_blockedCells[i], 0f);
        }
        _blockedCells.Clear();

        _intervalInProgress = false;
    }
    #endregion
}
