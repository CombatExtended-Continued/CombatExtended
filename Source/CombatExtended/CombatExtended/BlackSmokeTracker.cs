using System.Collections.Generic;
using System.Threading.Tasks;
using Verse;

namespace CombatExtended;

/// <summary>
/// Manages ticking for black smoke.
/// </summary>
public class BlackSmokeTracker(Map map) : MapComponent(map)
{
    private readonly List<Smoke> _smoke = [];

    public override void MapComponentTick()
    {
        Parallel.ForEach(_smoke, smoke => smoke.ParallelTick());

        // Apply previously calculated smoke spread.
        // This hopefully avoids destroying and recreating low-density smoke in the same cell within one tick.
        for (int i = 0; i < _smoke.Count; i++)
        {
            _smoke[i].DoSpreadToAdjacentCells();
        }
    }

    public void Register(Smoke smoke) => _smoke.Add(smoke);

    public void Unregister(Smoke smoke) => _smoke.Remove(smoke);
}
